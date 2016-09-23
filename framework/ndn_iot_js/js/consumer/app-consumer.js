var AppConsumer = function AppConsumer(face, keyChain, certificateName, doVerify)
{
    this.face = face;
    this.keyChain = keyChain;
    this.certificateName = certificateName;
    this.doVerify = doVerify;
}