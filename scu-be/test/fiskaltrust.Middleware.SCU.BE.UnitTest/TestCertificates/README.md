# Test Certificates

This directory contains test certificates for the Belgium ZwarteDoos SCU implementation.

## Certificate Files

### test-certificate.p12
Sample PKCS#12 certificate for testing digital signatures.
- Password: "test123"
- Validity: Test purposes only
- DO NOT USE IN PRODUCTION

### ca-certificates/
Contains sample Certificate Authority certificates for testing chain validation.

## Usage

These certificates are used for:
- Testing SSL/TLS connections to sandbox environments
- Testing digital signature functionality
- Validating certificate handling and error scenarios

## Security Note

⚠️ **WARNING**: These are test certificates only. Never use test certificates in production environments.

All certificates in this directory are:
- Self-signed or from test CAs
- Have weak security parameters
- Should only be used for development and testing