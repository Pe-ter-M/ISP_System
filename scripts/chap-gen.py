#!/usr/bin/env python3
"""Generate a FreeRADIUS radclient request file that authenticates via CHAP.

Usage:
    python3 scripts/chap-gen.py <username> <password>
    # stdout is a radclient request (User-Name, CHAP-Password, CHAP-Challenge)

CHAP (RFC 1994) is what many PPPoE routers send to the access concentrator,
which then forwards an Access-Request to RADIUS.  CHAP-Password is
MD5(identifier || password || challenge), with the identifier prepended.
"""
import hashlib
import os
import sys


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2

    username, password = sys.argv[1], sys.argv[2]

    # 1-byte CHAP identifier + 16-byte random challenge
    chap_id = int.from_bytes(os.urandom(1), "big")
    challenge = os.urandom(16)

    # CHAP response: MD5(identifier || password || challenge)
    response = hashlib.md5(bytes([chap_id]) + password.encode() + challenge).digest()

    # CHAP-Password attribute is 17 bytes: identifier followed by the response
    chap_password = bytes([chap_id]) + response

    print(f'User-Name = "{username}"')
    print(f"CHAP-Password = 0x{chap_password.hex()}")
    print(f"CHAP-Challenge = 0x{challenge.hex()}")
    print("NAS-IP-Address = 127.0.0.1")
    return 0


if __name__ == "__main__":
    sys.exit(main())
