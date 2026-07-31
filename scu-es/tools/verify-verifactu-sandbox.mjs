#!/usr/bin/env node
// Verify VeriFactu nature-of-VAT cases end-to-end against the fiskaltrust sandbox.
//
// For each supported NN case it POSTs a receipt to /v2/sign and then:
//   - on acceptance (ftState 0x0): reads the VeriFactu RegistroFacturacionAlta XML straight out of
//     ftStateData.ES.GovernmentAPI.Request (plain XML, no base64 wrapper unlike TicketBAI), writes
//     it to out-vf/<nn>-<name>.xml, and prints the spec codes
//     (L1 Impuesto, L8A ClaveRegimen, L9 CalificacionOperacion, L10 OperacionExenta);
//   - on rejection: prints the validation / AEAT error messages from ftSignatures[0].
//
// The XML is only returned when AEAT accepts (on rejection ftStateData.ES is empty). For the raw
// XML of a case the middleware can build but AEAT rejects, use the local generator instead
// (VeriFactuMappingXmlAcceptanceTests with UPDATE_GOLDEN=1).
//
// Credentials via environment (no token committed):
//   FT_CASHBOX   x-cashbox-id (the VeriFactu sandbox cashbox GUID)
//   FT_TOKEN     x-cashbox-accesstoken
//   FT_URL       optional, defaults to the sandbox sign endpoint
//
// Usage:  FT_CASHBOX=... FT_TOKEN=... node scu-es/tools/verify-verifactu-sandbox.mjs [nn ...]

import { randomUUID } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const URL = process.env.FT_URL ?? 'https://possystem-api-sandbox.fiskaltrust.eu/v2/sign';
const CASHBOX = process.env.FT_CASHBOX;
const TOKEN = process.env.FT_TOKEN;
if (!CASHBOX || !TOKEN) {
  console.error('Set FT_CASHBOX and FT_TOKEN (VeriFactu sandbox cashbox id + access token).');
  process.exit(2);
}

const OUT_DIR = join(dirname(fileURLToPath(import.meta.url)), 'out-vf');
mkdirSync(OUT_DIR, { recursive: true });

// ftReceiptCase: POS receipt (0x...0001) — simplified, no recipient required for VeriFactu.
const RECEIPT_CASE = 35184372088833;
// ftChargeItemCase = 0x2000_0000_0000 | nature(<<8) | vatCategory.
// Low nibble is the VAT-rate category and must match VATRate: NormalVatRate(0x3) for 21%,
// ZeroVatRate(0x7) for the 0% exempt/not-subject lines (bit 0x10 = service flag, from the sample).
const ci = (natureByte, exempt) => Number(0x200000000000n | (BigInt(natureByte) << 8n) | (exempt ? 0x17n : 0x13n));
const PAY_CASH = 35184372088833;

// nature = NN as hex (NN[10] -> 0x10 -> nature byte 0x1000). exempt: lines carry 0% VAT.
const CASES = [
  { nn: '00', name: 'usual-vat',            nature: 0x00, exempt: false },
  { nn: '10', name: 'exports',              nature: 0x10, exempt: true },
  { nn: '11', name: 'intra-community',      nature: 0x11, exempt: true },
  { nn: '13', name: 'treated-as-exports',   nature: 0x13, exempt: true },
  { nn: '14', name: 'customs',              nature: 0x14, exempt: true },
  { nn: '20', name: 'not-subject-location', nature: 0x20, exempt: true },
  { nn: '21', name: 'not-subject-art7',     nature: 0x21, exempt: true },
  { nn: '30', name: 'exempt-domestic',      nature: 0x30, exempt: true },
  { nn: '31', name: 'other-exemptions',     nature: 0x31, exempt: true },
  { nn: '50', name: 'reverse-charge',       nature: 0x50, exempt: true },
  { nn: '60', name: 'foreign-tax',          nature: 0x60, exempt: true },
  { nn: '80', name: 'excluded-third-party', nature: 0x80, exempt: true },
];

const only = process.argv.slice(2);
const selected = only.length ? CASES.filter(c => only.includes(c.nn)) : CASES;

const codes = (xml) => ({
  impuesto: [...xml.matchAll(/<Impuesto>([^<]+)/g)].map(m => m[1]).join(','),
  clave: [...xml.matchAll(/<ClaveRegimen>([^<]+)/g)].map(m => m[1]).join(','),
  calificacion: [...xml.matchAll(/<CalificacionOperacion>([^<]+)/g)].map(m => m[1]).join(','),
  exenta: [...xml.matchAll(/<OperacionExenta>([^<]+)/g)].map(m => m[1]).join(','),
});

const errors = (resp) => {
  const sig = (resp.ftSignatures ?? []).find(s => (s.ftSignatureType >>> 0) === 0x3000);
  if (!sig) return [];
  try { return JSON.parse(sig.Data).map(e => e.Message ?? e); } catch { return [sig.Data]; }
};

const summary = [];
for (const c of selected) {
  const item = c.exempt
    ? { Quantity: 1, Description: `NN[${c.nn}] ${c.name}`, Amount: 150, VATRate: 0, VATAmount: 0, ftChargeItemCase: ci(c.nature, true) }
    : { Quantity: 1, Description: `NN[${c.nn}] ${c.name}`, Amount: 121, VATRate: 21, VATAmount: 21, ftChargeItemCase: ci(c.nature, false) };
  const body = {
    ftReceiptCase: RECEIPT_CASE,
    cbReceiptReference: `VF-VERIFY-${c.nn}-${Date.now()}`,
    cbReceiptMoment: new Date(2026, 5, 14, 17, 25, 26).toISOString(),
    cbChargeItems: [item],
    cbPayItems: [{ Description: 'Cash', Amount: item.Amount, ftPayItemCase: PAY_CASH }],
  };
  let resp;
  try {
    const r = await fetch(URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-cashbox-id': CASHBOX,
        'x-cashbox-accesstoken': TOKEN,
        'x-operation-id': randomUUID(),
        'x-possystem-id': '00000000-0000-0000-0000-000000000000',
      },
      body: JSON.stringify(body),
    });
    resp = await r.json();
  } catch (e) {
    console.log(`NN[${c.nn}] ${c.name.padEnd(22)} HTTP ERROR ${e.message}`);
    summary.push({ nn: c.nn, status: 'http-error' });
    continue;
  }
  const accepted = (resp.ftState >>> 0) === 0x0;
  if (accepted) {
    const xml = resp?.ftStateData?.ES?.GovernmentAPI?.Request;
    if (xml) {
      writeFileSync(join(OUT_DIR, `${c.nn}-${c.name}.xml`), xml);
      const k = codes(xml);
      console.log(`NN[${c.nn}] ${c.name.padEnd(22)} ACCEPTED  L1=${k.impuesto} clave=${k.clave} calif=${k.calificacion || '-'} exenta=${k.exenta || '-'}`);
      summary.push({ nn: c.nn, status: 'accepted', ...k });
    } else {
      console.log(`NN[${c.nn}] ${c.name.padEnd(22)} ACCEPTED but no GovernmentAPI.Request`);
      summary.push({ nn: c.nn, status: 'accepted-no-xml' });
    }
  } else {
    const errs = errors(resp).map(m => String(m).split(';')[0].trim());
    console.log(`NN[${c.nn}] ${c.name.padEnd(22)} REJECTED  ${errs.join(' | ')}`);
    summary.push({ nn: c.nn, status: 'rejected', errors: errs });
  }
}

console.log(`\nXMLs written to ${OUT_DIR}`);
console.log(`Accepted: ${summary.filter(s => s.status === 'accepted').length}/${summary.length}`);
