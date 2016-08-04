var HmacHelper = function HmacHelper()
{

};

HmacHelper.generatePin = function()
{
    pin = "";
    for (var i = 0; i < 8; i++) {
        pin += String.fromCharCode(Math.round(Math.random() * 255));
    }
    return DataUtils.stringToHex(pin);
};

HmacHelper.signInterest = function(interest, key, keyName, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;

    // The random value is a TLV nonNegativeInteger too, but we know it is 8
    // bytes, so we don't need to call the nonNegativeInteger encoder.
    interest.getName().append(new Blob(Crypto.randomBytes(8), false));

    var timestamp = Math.round(new Date().getTime());
    // The timestamp is encoded as a TLV nonNegativeInteger.
    var encoder = new TlvEncoder(8);
    encoder.writeNonNegativeInteger(timestamp);
    interest.getName().append(new Blob(encoder.getOutput(), false));

    var s = new HmacWithSha256Signature();
    s.getKeyLocator().setType(KeyLocatorType.KEYNAME);
    s.getKeyLocator().setKeyName(keyName);
    
    interest.getName().append(wireFormat.encodeSignatureInfo(s));
    interest.getName().append(new Name.Component());

    var encoding = interest.wireEncode(wireFormat);
    var signer = Crypto.createHmac('sha256', key.buf());
    signer.update(encoding.signedBuf());
    s.setSignature(new Blob(signer.digest(), false));
    interest.setName(interest.getName().getPrefix(-1).append(wireFormat.encodeSignatureValue(s)));
}

HmacHelper.extractInterestSignature = function(interest, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;
    
    try {
        signature = wireFormat.decodeSignatureInfoAndValue(
                        interest.getName().get(-2).getValue().buf(),
                        interest.getName().get(-1).getValue().buf());
    } catch (e) {
        console.log(e);
        signature = null;
    }

    return signature;
}

HmacHelper.verifyInterest = function(interest, key, wireFormat)
{
    wireFormat = (typeof wireFormat === "function" || !wireFormat) ? WireFormat.getDefaultWireFormat() : wireFormat;

    var signature = HmacHelper.extractInterestSignature(interest, wireFormat);
    var encoding = interest.wireEncode(wireFormat);

    var signer = Crypto.createHmac('sha256', key.buf());
    signer.update(encoding.signedBuf());
    var newSignatureBits = new Blob(signer.digest(), false);
    console.log(newSignatureBits)
    // Use the flexible Blob.equals operator.
    return newSignatureBits.equals(signature.getSignature());
};
