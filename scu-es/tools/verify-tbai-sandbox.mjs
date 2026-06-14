#!/usr/bin/env node
// Verify TicketBAI nature-of-VAT cases end-to-end against the fiskaltrust sandbox.
//
// For each supported NN case it POSTs a receipt to /v2/sign and then:
//   - on acceptance (ftState 0x0): decodes the signed <t:TicketBai> document out of
//     ftStateData.ES.GovernmentAPI.Request (Batuz LROE-240 -> <FacturaEmitida><TicketBai> base64),
//     writes it to out/<nn>-<name>.xml, and prints the key spec codes;
//   - on rejection: prints the Bizkaia (B4_*) error messages from ftSignatures[0].
//
// The generated XML is ONLY returned by the sandbox when the regional endpoint accepts the
// receipt (on rejection ftStateData.ES is empty). To inspect the XML for a case the middleware
// can build but Bizkaia rejects, use the local generator instead (TicketBaiFactoryXmlAcceptanceTests
// with UPDATE_TICKETBAI_SNAPSHOTS=1).
//
// Credentials are read from the environment so no token is committed:
//   FT_CASHBOX   x-cashbox-id        (GUID)
//   FT_TOKEN     x-cashbox-accesstoken
//   FT_URL       optional, defaults to the sandbox sign endpoint
//
// Usage (Git Bash / PowerShell):
//   FT_CASHBOX=... FT_TOKEN=... node scu-es/tools/verify-tbai-sandbox.mjs [nn ...]
//   (optional positional args limit the run to specific NN codes, e.g. `... 10 30 60`)

import { randomUUID } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const URL = process.env.FT_URL ?? 'https://possystem-api-sandbox.fiskaltrust.eu/v2/sign';
const CASHBOX = process.env.FT_CASHBOX;
const TOKEN = process.env.FT_TOKEN;
if (!CASHBOX || !TOKEN) {
  console.error('Set FT_CASHBOX and FT_TOKEN environment variables (sandbox cashbox id + access token).');
  process.exit(2);
}

const OUT_DIR = join(dirname(fileURLToPath(import.meta.url)), 'out');
mkdirSync(OUT_DIR, { recursive: true });

// ftReceiptCase: B2B invoice (0x...1002) — non-simplified, carries a recipient.
const RECEIPT_CASE = 35184372092930n;
// ftChargeItemCase base: 0x2000_0000_0018; nature byte (mask 0xFF00) is OR'd in per case.
const CI_BASE = 0x200000000018n;
const ci = (natureByte) => Number(CI_BASE | (BigInt(natureByte) << 8n));
const PAY_CASH = 35184372088833;

// Domestic recipient (valid Spanish NIF, checksum-correct) -> DesgloseFactura.
const DOMESTIC = { CustomerVATId: '12345678Z', CustomerName: 'Cliente Nacional SL', CustomerStreet: 'Calle Mayor 1', CustomerZip: '48001', CustomerCity: 'Bilbao', CustomerCountry: 'ES' };
// Foreign recipient (CustomerTaxId -> IDOtro IDType=04, looser format rules) -> DesgloseTipoOperacion.
const FOREIGN = { CustomerTaxId: 'DE811569869', CustomerName: 'Foreign Buyer GmbH', CustomerStreet: 'Hauptstr 1', CustomerZip: '10115', CustomerCity: 'Berlin', CustomerCountry: 'DE' };

// nn = the spec NN code; nature = ftChargeItemCase nature byte (= NN as hex, e.g. NN[10] -> 0x10 -> 0x1000).
const CASES = [
  { nn: '10', name: 'exports',                 nature: 0x10, customer: FOREIGN },
  { nn: '11', name: 'intra-community',         nature: 0x11, customer: FOREIGN },
  { nn: '13', name: 'treated-as-exports',      nature: 0x13, customer: FOREIGN },
  { nn: '14', name: 'customs',                 nature: 0x14, customer: FOREIGN },
  { nn: '20', name: 'not-subject-location',    nature: 0x20, customer: DOMESTIC },
  { nn: '21', name: 'not-subject-art7',        nature: 0x21, customer: DOMESTIC },
  { nn: '30', name: 'exempt-domestic',         nature: 0x30, customer: DOMESTIC },
  { nn: '31', name: 'other-exemptions',        nature: 0x31, customer: DOMESTIC },
  { nn: '50', name: 'reverse-charge',          nature: 0x50, customer: FOREIGN },
  { nn: '60', name: 'foreign-tax-ipsi-igic',   nature: 0x60, customer: DOMESTIC },
  { nn: '80', name: 'excluded-third-party',    nature: 0x80, customer: DOMESTIC },
];

const only = process.argv.slice(2);
const selected = only.length ? CASES.filter(c => only.includes(c.nn)) : CASES;

const codes = (xml) => ({
  clave: [...xml.matchAll(/<ClaveRegimenIvaOpTrascendencia>([^<]+)/g)].map(m => m[1]).join(','),
  causaExencion: [...xml.matchAll(/<CausaExencion>([^<]+)/g)].map(m => m[1]).join(','),
  tipoNoExenta: [...xml.matchAll(/<TipoNoExenta>([^<]+)/g)].map(m => m[1]).join(','),
  causaNoSujeta: [...xml.matchAll(/<DetalleNoSujeta>[\s\S]*?<Causa>([^<]+)/g)].map(m => m[1]).join(','),
  branch: [xml.includes('<DesgloseTipoOperacion>') ? 'DesgloseTipoOperacion' : null,
           xml.includes('<DesgloseFactura>') ? 'DesgloseFactura' : null,
           xml.includes('<Entrega>') ? 'Entrega' : null,
           xml.includes('<PrestacionServicios>') ? 'PrestacionServicios' : null,
           xml.includes('<Exenta>') ? 'Exenta' : null,
           xml.includes('<NoSujeta>') ? 'NoSujeta' : null,
           /<NoExenta>/.test(xml) ? 'NoExenta' : null].filter(Boolean).join('/'),
});

const extractTbai = (resp) => {
  const req = resp?.ftStateData?.ES?.GovernmentAPI?.Request;
  if (!req) return null;
  const m = req.match(/<TicketBai>([^<]+)<\/TicketBai>/);
  if (!m) return null;
  return Buffer.from(m[1], 'base64').toString('utf8');
};

const errors = (resp) => {
  const sig = (resp.ftSignatures ?? []).find(s => (s.ftSignatureType >>> 0) === 0x3000);
  if (!sig) return [];
  try { return JSON.parse(sig.Data).map(e => e.Message); } catch { return [sig.Data]; }
};

const summary = [];
for (const c of selected) {
  const body = {
    ftReceiptCase: Number(RECEIPT_CASE),
    cbReceiptReference: `TBAI-VERIFY-${c.nn}-${Date.now()}`,
    cbReceiptMoment: new Date(2026, 5, 14, 16, 25, 6).toISOString(),
    cbCustomer: c.customer,
    cbChargeItems: [{ Quantity: 1, Description: `NN[${c.nn}] ${c.name}`, Amount: 150, VATRate: 0, VATAmount: 0, ftChargeItemCase: ci(c.nature) }],
    cbPayItems: [{ Description: 'Cash', Amount: 150, ftPayItemCase: PAY_CASH }],
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
  const state = (resp.ftState >>> 0);
  const accepted = state === 0x0;
  if (accepted) {
    const xml = extractTbai(resp);
    if (xml) {
      const file = join(OUT_DIR, `${c.nn}-${c.name}.xml`);
      writeFileSync(file, xml);
      const k = codes(xml);
      console.log(`NN[${c.nn}] ${c.name.padEnd(22)} ACCEPTED  clave=${k.clave} exenta=${k.causaExencion||'-'} tipoNoExenta=${k.tipoNoExenta||'-'} noSujeta=${k.causaNoSujeta||'-'}  ${k.branch}`);
      summary.push({ nn: c.nn, status: 'accepted', ...k });
    } else {
      console.log(`NN[${c.nn}] ${c.name.padEnd(22)} ACCEPTED but no GovernmentAPI.Request found`);
      summary.push({ nn: c.nn, status: 'accepted-no-xml' });
    }
  } else {
    const errs = errors(resp).map(m => m.split(';')[0].trim());
    console.log(`NN[${c.nn}] ${c.name.padEnd(22)} REJECTED  ${errs.join(' | ')}`);
    summary.push({ nn: c.nn, status: 'rejected', errors: errs });
  }
}

console.log(`\nXMLs written to ${OUT_DIR}`);
const ok = summary.filter(s => s.status === 'accepted').length;
console.log(`Accepted: ${ok}/${summary.length}`);
