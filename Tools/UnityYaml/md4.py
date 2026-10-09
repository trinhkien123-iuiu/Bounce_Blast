"""MD4 thuần Python (hashlib của OpenSSL 3 đã tắt MD4). Dùng để tính fileID của script nằm trong DLL:
   fileID = int32 little-endian của MD4(b"s\0\0\0" + namespace + className)."""
import struct


def _lrot(x, n):
    x &= 0xFFFFFFFF
    return ((x << n) | (x >> (32 - n))) & 0xFFFFFFFF


def md4(data: bytes) -> bytes:
    msg = bytearray(data)
    ml = len(data) * 8
    msg.append(0x80)
    while len(msg) % 64 != 56:
        msg.append(0)
    msg += struct.pack("<Q", ml)
    a, b, c, d = 0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476
    F = lambda x, y, z: (x & y) | (~x & z)
    G = lambda x, y, z: (x & y) | (x & z) | (y & z)
    H = lambda x, y, z: x ^ y ^ z
    for off in range(0, len(msg), 64):
        X = struct.unpack("<16I", msg[off:off + 64])
        aa, bb, cc, dd = a, b, c, d
        for i in (0, 4, 8, 12):
            a = _lrot(a + F(b, c, d) + X[i], 3)
            d = _lrot(d + F(a, b, c) + X[i + 1], 7)
            c = _lrot(c + F(d, a, b) + X[i + 2], 11)
            b = _lrot(b + F(c, d, a) + X[i + 3], 19)
        for i in (0, 1, 2, 3):
            a = _lrot(a + G(b, c, d) + X[i] + 0x5A827999, 3)
            d = _lrot(d + G(a, b, c) + X[i + 4] + 0x5A827999, 5)
            c = _lrot(c + G(d, a, b) + X[i + 8] + 0x5A827999, 9)
            b = _lrot(b + G(c, d, a) + X[i + 12] + 0x5A827999, 13)
        for i in (0, 2, 1, 3):
            a = _lrot(a + H(b, c, d) + X[i] + 0x6ED9EBA1, 3)
            d = _lrot(d + H(a, b, c) + X[i + 8] + 0x6ED9EBA1, 9)
            c = _lrot(c + H(d, a, b) + X[i + 4] + 0x6ED9EBA1, 11)
            b = _lrot(b + H(c, d, a) + X[i + 12] + 0x6ED9EBA1, 15)
        a, b, c, d = (a + aa) & 0xFFFFFFFF, (b + bb) & 0xFFFFFFFF, (c + cc) & 0xFFFFFFFF, (d + dd) & 0xFFFFFFFF
    return struct.pack("<4I", a, b, c, d)


def script_file_id(namespace: str, class_name: str) -> int:
    h = md4(b"s\0\0\0" + (namespace + class_name).encode("utf-8"))
    return struct.unpack("<i", h[:4])[0]


if __name__ == "__main__":
    assert md4(b"abc").hex() == "a448017aaf21d8525fc10ae87aa6729d", md4(b"abc").hex()
    print("MD4 ok")
    for ns, n in [("UnityEngine.UI", "Image"), ("UnityEngine.UI", "Button"), ("TMPro", "TextMeshProUGUI"), ("TMPro", "TMP_FontAsset")]:
        print(ns, n, script_file_id(ns, n))
