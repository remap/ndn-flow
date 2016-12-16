/* -*- Mode:C++; c-file-style:"gnu"; indent-tabs-mode:nil -*- */
/**
 * Copyright (C) 2016 Regents of the University of California.
 * @author: Jeff Thompson <jefft0@remap.ucla.edu>
 *          Zhehao Wang <zhehao@cs.ucla.edu>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version, with the additional exemption that
 * compiling, linking, and/or using OpenSSL is allowed.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * A copy of the GNU Lesser General Public License is in the file COPYING.
 */

// Note: To compile this sketch, you must fix NDN_CPP_ROOT in ndn_cpp_root.h .

#include <RFduinoBLE.h>
#include <ndn-cpp/lite/data-lite.hpp>
#include <ndn-cpp/lite/interest-lite.hpp>
#include <ndn-cpp/lite/encoding/tlv-0_2-wire-format-lite.hpp>
#include <ndn-cpp/lite/util/dynamic-uint8-array-lite.hpp>
#include <ndn-cpp/lite/util/crypto-lite.hpp>
#include "rsaref-crypto.h"
#include "rsaref-rsa-public-key-lite.hpp"

#include <Wire.h>

#include "gyro.h"

using namespace ndn;

uint8_t HmacKey[64];
uint8_t HmacKeyDigest[ndn_SHA256_DIGEST_SIZE];
R_RANDOM_STRUCT RandomStruct;

// Connected or no
bool connected = false;
unsigned long currentIdx = 0;

int currentCycle = 0;

static void printHex(const uint8_t* buffer, size_t bufferLength)
{
  for (size_t i = 0; i < bufferLength; ++i) {
    char buf[5];
    sprintf(buf, " %02x", (int)buffer[i]);
    Serial.print(buf);
  }
}

static R_RSA_PUBLIC_KEY CONSUMER_E_KEY = {
  2048,
  { 0xb8, 0x09, 0xa7, 0x59, 0x82, 0x84, 0xec, 0x4f, 0x06, 0xfa, 0x1c, 0xb2, 0xe1, 0x38, 0x93, 0x53,
    0xbb, 0x7d, 0xd4, 0xac, 0x88, 0x1a, 0xf8, 0x25, 0x11, 0xe4, 0xfa, 0x1d, 0x61, 0x24, 0x5b, 0x82,
    0xca, 0xcd, 0x72, 0xce, 0xdb, 0x66, 0xb5, 0x8d, 0x54, 0xbd, 0xfb, 0x23, 0xfd, 0xe8, 0x8e, 0xaf,
    0xa7, 0xb3, 0x79, 0xbe, 0x94, 0xb5, 0xb7, 0xba, 0x17, 0xb6, 0x05, 0xae, 0xce, 0x43, 0xbe, 0x3b,
    0xce, 0x6e, 0xea, 0x07, 0xdb, 0xbf, 0x0a, 0x7e, 0xeb, 0xbc, 0xc9, 0x7b, 0x62, 0x3c, 0xf5, 0xe1,
    0xce, 0xe1, 0xd9, 0x8d, 0x9c, 0xfe, 0x1f, 0xc7, 0xf8, 0xfb, 0x59, 0xc0, 0x94, 0x0b, 0x2c, 0xd9,
    0x7d, 0xbc, 0x96, 0xeb, 0xb8, 0x79, 0x22, 0x8a, 0x2e, 0xa0, 0x12, 0x1d, 0x42, 0x07, 0xb6, 0x5d,
    0xdb, 0xe1, 0xf6, 0xb1, 0x5d, 0x7b, 0x1f, 0x54, 0x52, 0x1c, 0xa3, 0x11, 0x9b, 0xf9, 0xeb, 0xbe,
    0xb3, 0x95, 0xca, 0xa5, 0x87, 0x3f, 0x31, 0x18, 0x1a, 0xc9, 0x99, 0x01, 0xec, 0xaa, 0x90, 0xfd,
    0x8a, 0x36, 0x35, 0x5e, 0x12, 0x81, 0xbe, 0x84, 0x88, 0xa1, 0x0d, 0x19, 0x2a, 0x4a, 0x66, 0xc1,
    0x59, 0x3c, 0x41, 0x83, 0x3d, 0x3d, 0xb8, 0xd4, 0xab, 0x34, 0x90, 0x06, 0x3e, 0x1a, 0x61, 0x74,
    0xbe, 0x04, 0xf5, 0x7a, 0x69, 0x1b, 0x9d, 0x56, 0xfc, 0x83, 0xb7, 0x60, 0xc1, 0x5e, 0x9d, 0x85,
    0x34, 0xfd, 0x02, 0x1a, 0xba, 0x2c, 0x09, 0x72, 0xa7, 0x4a, 0x5e, 0x18, 0xbf, 0xc0, 0x58, 0xa7,
    0x49, 0x34, 0x46, 0x61, 0x59, 0x0e, 0xe2, 0x6e, 0x9e, 0xd2, 0xdb, 0xfd, 0x72, 0x2f, 0x3c, 0x47,
    0xcc, 0x5f, 0x99, 0x62, 0xee, 0x0d, 0xf3, 0x1f, 0x30, 0x25, 0x20, 0x92, 0x15, 0x4b, 0x04, 0xfe,
    0x15, 0x19, 0x1d, 0xdc, 0x7e, 0x5c, 0x10, 0x21, 0x52, 0x21, 0x91, 0x54, 0x60, 0x8b, 0x92, 0x41
  },
  { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01
  }
};

void setup()
{
  // Enable serial debug, type cntrl-M at runtime.
  Serial.begin(9600);
  while (!Serial); // Wait untilSerial is ready.
  Serial.println("RFduino started");

  // configure the RFduino BLE properties
  RFduinoBLE.advertisementData = "ledbtn";
  RFduinoBLE.advertisementInterval = 500;
  RFduinoBLE.deviceName = "RFduino";
  RFduinoBLE.txPowerLevel = -20;
  Serial.println("RFduino BLE Advertising interval is 500ms");
  Serial.println("RFduino BLE DeviceName: RFduino");

  // Generate a random seed.
  // Turn on the random number generator.
  NRF_RNG->TASKS_START = 1;
  while (ndn_RsarefCrypto_getRandomBytesNeeded() > 0) {
    // Clear the ready flag and wait for a value.
    NRF_RNG->EVENTS_VALRDY = 0;
    while (NRF_RNG->EVENTS_VALRDY == 0);
    unsigned char seed = NRF_RNG->VALUE;
    ndn_RsarefCrypto_randomUpdate(&seed, 1);
  }

  // Turn off the random number generator since it doesn't work when BLE is enabled.
  NRF_RNG->TASKS_STOP = 1;
  
  // Generate the HmacKey.
  CryptoLite::generateRandomBytes(HmacKey, sizeof(HmacKey));
  // Set HmacKeyDigest to sha256(HmacKey).
  CryptoLite::digestSha256(HmacKey, sizeof(HmacKey), HmacKeyDigest);

#if 1
  // Encrypt the HmacKey for the CONSUMER_E_KEY.
  RsarefRsaPublicKeyLite publicKeyLite(CONSUMER_E_KEY);
  uint8_t encryptedHmacKey[MAX_ENCRYPTED_KEY_LEN];
  size_t encryptedHmacKeyLength;
  ndn_Error error;
  error = publicKeyLite.encrypt
          (HmacKey, sizeof(HmacKey), ndn_EncryptAlgorithmType_RsaPkcs,
           encryptedHmacKey, encryptedHmacKeyLength);
  if (error) {
    // We don't expect this to happen.
    Serial.print("Error encrypting the HmacKey: ");
    Serial.println(error);
  }
  else {
    Serial.print("encryptedHmacKey");
    printHex(encryptedHmacKey, encryptedHmacKeyLength); 
    Serial.println("");
  }
#endif

  // start the BLE stack
  RFduinoBLE.begin();
  Serial.println("RFduino BLE stack started");

  // set up gyroscope publisher
  setupGyroProducer();
}

int count = 0;

void 
setupGyroProducer()
{ 
  Wire.begin();
  delay(1);
  check_MPU();
  
  Serial.println("MPU-6050 6-Axis");

  regWrite(0x6B, 0xC0);
  regWrite(0x6C, 0x00);
  delay(10);
  
// regWrite(0x6B, 0x70);
  regWrite(0x6B, 0x00);
  regWrite(0x6D, 0x70);
  regWrite(0x6E, 0x06);
  temp = regRead(0x6F);
  Serial.print("Bank 1, Reg 6 = ");
  Serial.println(temp, HEX);

// temp = regRead(0x6B);
// Serial.println(temp, HEX);
  
  regWrite(0x6D, 0x00);
  
  temp = regRead(0x00);
  Serial.println(temp, HEX);
  temp = regRead(0x01);
  Serial.println(temp, HEX);
  temp = regRead(0x02);
  Serial.println(temp, HEX);
  temp = regRead(0x6A);
  Serial.println(temp, HEX);
  
  regWrite(0x37, 0x32);
  
  temp = regRead(0x6B);
  Serial.println(temp, HEX);
  delay(5);
// regWrite(0x25, 0x68); //Set Slave 0 to self
//
// regWrite(0x6A, 0x02);

  mem_init();
  delay(20);
}

void
loop()
{
  // always report gyro reading, even if no rpi's connected; 
  // but only send stuff out on ble when something's connected
  updateGyro(); 
}

void
RFduinoBLE_onAdvertisement()
{
  Serial.println("RFduino is doing BLE advertising ...");
}

void
RFduinoBLE_onConnect()
{
  Serial.println("RFduino BLE connection successful");
  connected = true;
  resetFifo();
}

void
RFduinoBLE_onDisconnect()
{
  Serial.println("RFduino BLE disconnected");
  connected = false;
  resetFifo();
}

static void
printBlobRawString(const ndn::BlobLite& blob)
{
  for (size_t i = 0; i < blob.size(); ++i)
    Serial.print((char)blob.buf()[i]);
}

static void
simplePrintNameUri(const ndn::NameLite& name)
{
  for (size_t i = 0; i < name.size(); ++i) {
    Serial.print("/");
    printBlobRawString(name.get(i).getValue());
  }
}

static void
onReceivedElement(const uint8_t *element, size_t elementLength);
static void
processInterest(const InterestLite& interest);
static void
fragmentAndSend(const uint8_t* buffer, size_t bufferLength);

// TODO: Determine the correct max size of a received packet.
uint8_t receiveBuffer[100];
size_t receiveBufferLength = 0;
int expectedFragmentIndex = 0;

void
RFduinoBLE_onReceive(char *buffer, int bufferLength)
{
  Serial.print("Debug: Received data over BLE:");
  printHex((const uint8_t*)buffer, bufferLength);
  Serial.println("");

  // Defragment into the receiveBuffer.
  const size_t iFragmentIndex = 0;
  const size_t iFragmentCount = 1;
  const size_t iFragment = 2;
  if (bufferLength < iFragment + 1)
    // We don't even have one fragment byte.
    return;
  int fragmentIndex = buffer[iFragmentIndex];
  if (fragmentIndex == 0)
    // Starting a new fragmented packet.
    receiveBufferLength = 0;
  else {
    if (fragmentIndex != expectedFragmentIndex) {
      // Assume we have a dropped fragment. Wait for a new fragment index 0.
      Serial.println("Unexpected fragment index. Dropped. Waiting for the start of the next packet.");
      expectedFragmentIndex = -1;
      return;
    }
  }

  size_t nFragmentBytes = bufferLength - iFragment;
  if (receiveBufferLength + nFragmentBytes > sizeof(receiveBuffer)) {
    Serial.println("Error: Exceeded sizeof(receiveBuffer)");
    return;
  }
  memset(receiveBuffer + receiveBufferLength, 0, 20);
  memcpy(receiveBuffer + receiveBufferLength, buffer + iFragment, nFragmentBytes);
  receiveBufferLength += nFragmentBytes;

  expectedFragmentIndex = fragmentIndex + 1;
  if (expectedFragmentIndex >= buffer[iFragmentCount]) {
    // Finished. With the fragmentation protocol, we don't need an ElementReader, so
    // call onReceivedElement right away.
    expectedFragmentIndex = 0;
    onReceivedElement(receiveBuffer, receiveBufferLength);
  }
}

static void
onReceivedElement(const uint8_t *element, size_t elementLength)
{
  const int interestTlvType = 5;
  if (element[0] == interestTlvType) {
    // Decode the element as an InterestLite.
    ndn_NameComponent interestNameComponents[10];
    struct ndn_ExcludeEntry excludeEntries[2];
    InterestLite interest
      (interestNameComponents, sizeof(interestNameComponents) / sizeof(interestNameComponents[0]), 
       excludeEntries, sizeof(excludeEntries) / sizeof(excludeEntries[0]), 0, 0);
    size_t signedPortionBeginOffset, signedPortionEndOffset;
    ndn_Error error;
    if ((error = Tlv0_2WireFormatLite::decodeInterest
                 (interest, element, elementLength, &signedPortionBeginOffset,
                  &signedPortionEndOffset))) {
      Serial.println("Error decoding the received interest");
      return;
    }

    processInterest(interest);
  }
}

static void
processInterest(const InterestLite& interest)
{
  Serial.print("Debug: Received interest ");
  simplePrintNameUri(interest.getName());
  Serial.println("");
}

/**
 * Sign the data with HmacKey (updating the data object) and send.
 * @return 0 for success or an ndn_Error code.
 */
static ndn_Error
signAndSendData(DataLite& data)
{
  // Set up the signature with the hmacKeyDigest key locator digest.
  data.getSignature().setType(ndn_SignatureType_HmacWithSha256Signature);
  data.getSignature().getKeyLocator().setType(ndn_KeyLocatorType_KEY_LOCATOR_DIGEST);
  data.getSignature().getKeyLocator().setKeyData(BlobLite(HmacKeyDigest, sizeof(HmacKeyDigest)));

  // Encode once to get the signed portion.
  // TODO: Choose the correct max encoding buffer size.
  uint8_t encoding[120];
  DynamicUInt8ArrayLite output(encoding, sizeof(encoding), 0);
  ndn_Error error;
  size_t signedPortionBeginOffset, signedPortionEndOffset;
  size_t encodingLength;
  if ((error = Tlv0_2WireFormatLite::encodeData
               (data, &signedPortionBeginOffset, &signedPortionEndOffset, output, &encodingLength)))
    return error;

  // Get the signature for the signed portion.
  uint8_t signatureValue[ndn_SHA256_DIGEST_SIZE];
  CryptoLite::computeHmacWithSha256
  (HmacKey, sizeof(HmacKey), encoding + signedPortionBeginOffset,
   signedPortionEndOffset - signedPortionBeginOffset, signatureValue);
  data.getSignature().setSignature(BlobLite(signatureValue, ndn_SHA256_DIGEST_SIZE));

  // Encode again to include the signature.
  if ((error = Tlv0_2WireFormatLite::encodeData
               (data, &signedPortionBeginOffset, &signedPortionEndOffset, output, &encodingLength)))
    return error;

  fragmentAndSend(encoding, encodingLength);
  return NDN_ERROR_success;
}

/**
 * Fragment the buffer and send over BLE.
 */
static void
fragmentAndSend(const uint8_t* buffer, size_t bufferLength)
{
  const size_t iFragmentIndex = 0;
  const size_t iFragmentCount = 1;
  const size_t iFragment = 2;
  const size_t maxFragmentBytes = 18;
  size_t fragmentCount = (bufferLength - 1) / maxFragmentBytes + 1;

  uint8_t packet[2 + maxFragmentBytes];
  packet[iFragmentIndex] = 0;
  packet[iFragmentCount] = (uint8_t)fragmentCount;
  for (size_t i = 0; i < bufferLength; i += maxFragmentBytes) {
      // Copy the fragment bytes into the packet.
      size_t nFragmentBytes = maxFragmentBytes;
      if (i + nFragmentBytes > bufferLength)
          nFragmentBytes = bufferLength - i;
      memcpy(packet + iFragment, buffer + i, nFragmentBytes);
      // send as suggested by the BulkDataTransfer example: https://github.com/RFduino/RFduino/blob/master/libraries/RFduinoBLE/examples/BulkDataTransfer/BulkDataTransfer.ino
      //while (!RFduinoBLE.send((const char *) packet, iFragment + nFragmentBytes))
      RFduinoBLE.send((const char *) packet, iFragment + nFragmentBytes);
      
      // Increment the fragment index in the packet.
      ++packet[iFragmentIndex];
  }
}

/*******
 * Functions related with gyro publishing
 */

void dmp_init(){
  
  for(int i = 0; i < 7; i++){
    bank_sel(i);
    for(byte j = 0; j < 16; j++){
      
      byte start_addy = j * 0x10;
      
      Wire.beginTransmission(MPU_ADDR);
      Wire.write(MEM_START_ADDR);
      Wire.write(start_addy);
      Wire.endTransmission();
  
      Wire.beginTransmission(MPU_ADDR);
      Wire.write(MEM_R_W);
      for(int k = 0; k < 16; k++){
        unsigned char byteToSend = pgm_read_byte(&(dmpMem[i][j][k]));
        Wire.write((byte) byteToSend);
      }
      Wire.endTransmission();
    }
  }
  
  bank_sel(7);

  for(byte j = 0; j < 8; j++){
    
    byte start_addy = j * 0x10;
    
    Wire.beginTransmission(MPU_ADDR);
    Wire.write(MEM_START_ADDR);
    Wire.write(start_addy);
    Wire.endTransmission();

    Wire.beginTransmission(MPU_ADDR);
    Wire.write(MEM_R_W);
    for(int k = 0; k < 16; k++){
      unsigned char byteToSend = pgm_read_byte(&(dmpMem[7][j][k]));
      Wire.write((byte) byteToSend);
    }
    Wire.endTransmission();
  }
  
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(MEM_START_ADDR);
  Wire.write(0x80);
  Wire.endTransmission();
  
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(MEM_R_W);
  for(int k = 0; k < 9; k++){
      unsigned char byteToSend = pgm_read_byte(&(dmpMem[7][8][k]));
      Wire.write((byte) byteToSend);
  }
  Wire.endTransmission();
  
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(MEM_R_W);
  Wire.endTransmission();
  Wire.beginTransmission(MPU_ADDR);
  Wire.requestFrom(MPU_ADDR,9);
// Wire.endTransmission();
  byte incoming[9];
  for(int i = 0; i < 9; i++){
    incoming[i] = Wire.read();
  }
}
  
void mem_init(){
  
  dmp_init();
  
  for(byte i = 0; i < 22; i++){
    bank_sel(dmp_updates[i][0]);
    Wire.beginTransmission(MPU_ADDR);
    Wire.write(MEM_START_ADDR);
    Wire.write(dmp_updates[i][1]);
    Wire.endTransmission();

    Wire.beginTransmission(MPU_ADDR);
    Wire.write(MEM_R_W);
    for(byte j = 0; j < dmp_updates[i][2]; j++){
      Wire.write(dmp_updates[i][j+3]);
    }
    Wire.endTransmission();
  }

  regWrite(0x38, 0x32);

  for(byte i = 22; i < 29; i++){
    bank_sel(dmp_updates[i][0]);
    Wire.beginTransmission(MPU_ADDR);
    Wire.write(MEM_START_ADDR);
    Wire.write(dmp_updates[i][1]);
    Wire.endTransmission();

    Wire.beginTransmission(MPU_ADDR);
    Wire.write(MEM_R_W);
    for(byte j = 0; j < dmp_updates[i][2]; j++){
      Wire.write(dmp_updates[i][j+3]);
    }
    Wire.endTransmission();
  }
  
  temp = regRead(0x6B);
  Serial.println(temp, HEX);
  temp = regRead(0x6C);
  Serial.println(temp, HEX);
  
  regWrite(0x38, 0x02);
  regWrite(0x6B, 0x03);
  regWrite(0x19, 0x04);
  regWrite(0x1B, 0x18);
  regWrite(0x1A, 0x0B);
  regWrite(0x70, 0x03);
  regWrite(0x71, 0x00);
  regWrite(0x00, 0x00);
  regWrite(0x01, 0x00);
  regWrite(0x02, 0x00);
  
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x13);
  for(byte i = 0; i < 6; i++){
    Wire.write(0x00);
  }
  Wire.endTransmission();
  
// regWrite(0x24, 0x00);

  bank_sel(0x01);
  regWrite(0x6E, 0xB2);
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6F);
  Wire.write(0xFF); Wire.write(0xFF);
  Wire.endTransmission();

  bank_sel(0x01);
  regWrite(0x6E, 0x90);
  
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6F);
  Wire.write(0x09); Wire.write(0x23); Wire.write(0xA1); Wire.write(0x35);
  Wire.endTransmission();
  
  temp = regRead(0x6A);
  
  regWrite(0x6A, 0x04);
  
  //Insert FIFO count read?
  fifoReady();
  
  regWrite(0x6A, 0x00);
  regWrite(0x6B, 0x03);
  
  delay(2);
  
  temp = regRead(0x6C);
// Serial.println(temp, HEX);
  regWrite(0x6C, 0x00);
  temp = regRead(0x1C);
// Serial.println(temp, HEX);
  regWrite(0x1C, 0x00);
  delay(2);
  temp = regRead(0x6B);
// Serial.println(temp, HEX);
  regWrite(0x1F, 0x02);
  regWrite(0x21, 0x9C);
  regWrite(0x20, 0x50);
  regWrite(0x22, 0x00);
  regWrite(0x6A, 0x04);
  regWrite(0x6A, 0x00);
  regWrite(0x6A, 0xC8);
  
  bank_sel(0x01);
  regWrite(0x6E, 0x6A);
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6F);
  Wire.write(0x06); Wire.write(0x00);
  Wire.endTransmission();
  
  bank_sel(0x01);
  regWrite(0x6E, 0x60);
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6F);
  for(byte i = 0; i < 8; i++){
    Wire.write(0x00);
  }
  Wire.endTransmission();
  bank_sel(0x00);
  regWrite(0x6E, 0x60);
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6F);
  Wire.write(0x40); Wire.write(0x00); Wire.write(0x00); Wire.write(0x00);
  Wire.endTransmission();  
}

void regWrite(byte addy, byte regUpdate){
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(addy);
  Wire.write(regUpdate);
  Wire.endTransmission();
}

byte regRead(byte addy){
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(addy);
  Wire.endTransmission();
  Wire.beginTransmission(MPU_ADDR);
  Wire.requestFrom(MPU_ADDR,1);

  while(!Wire.available()){
  }
  byte incoming = Wire.read();
  return incoming;
}

void getPacket(){
  if(fifoCountL > 32){
    fifoCountL2 = fifoCountL - 32;
    longPacket = true;
  }
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x74);
  Wire.endTransmission();
  if(longPacket){
    Wire.beginTransmission(MPU_ADDR);
    Wire.requestFrom(MPU_ADDR, 32);
    for(byte i = 0; i < 32; i++){
      received_packet[i] = Wire.read();
    }
    Wire.beginTransmission(MPU_ADDR);
    Wire.write(0x74);
    Wire.endTransmission();
    Wire.beginTransmission(MPU_ADDR);
    Wire.requestFrom(MPU_ADDR, (unsigned int)fifoCountL2);
    for(byte i = 32; i < fifoCountL; i++){
      received_packet[i] = Wire.read();
    }
    longPacket = false;
  }else{
    Wire.beginTransmission(MPU_ADDR);
    Wire.requestFrom(MPU_ADDR, (unsigned int)fifoCountL);
    for(byte i = 0; i < fifoCountL; i++){
      received_packet[i] = Wire.read();
    }
  }
}

byte read_interrupt(){
  byte int_status = regRead(0x3A);
  return int_status;
}

boolean fifoReady(){
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x72);
  Wire.endTransmission();
  Wire.beginTransmission(MPU_ADDR);
  Wire.requestFrom(MPU_ADDR,2);
  byte fifoCountH = Wire.read();
  fifoCountL = Wire.read();
  if(fifoCountL == 42 || fifoCountL == 44){
    return 1;
  }
  else return 0;
}

void resetFifo(){
  byte ctrl = regRead(0x6A);
  ctrl |= 0b00000100;
  regWrite(0x6A, ctrl);
}

void updateGyro(){
  if(millis() >= lastRead + GYRO_UPDATE_INTERVAL){
    lastRead = millis();
    if(fifoReady()){
      getPacket();
      temp = regRead(0x3A);
      if(firstPacket){
        delay(1);
        bank_sel(0x00);
        regWrite(0x6E, 0x60);
        Wire.beginTransmission(MPU_ADDR);
        Wire.write(0x6F);
        Wire.write(0x04); Wire.write(0x00); Wire.write(0x00); Wire.write(0x00);
        Wire.endTransmission();
        bank_sel(1);
        regWrite(0x6E, 0x62);
        Wire.beginTransmission(MPU_ADDR);
        Wire.write(0x6F);
        Wire.endTransmission();
        Wire.beginTransmission(MPU_ADDR);
        Wire.requestFrom(MPU_ADDR,2);
        temp = Wire.read();
        temp = Wire.read();
        firstPacket = false;
        
        fifoReady();
      }

      if(fifoCountL == 42){
        if (currentCycle == CYCLE_THRESHOLD) {
          processQuat();
          sendQuat();
          currentCycle = 0;
        } else {
          currentCycle += 1;
        }
      }
    }
  }

}

void check_MPU(){
    
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x75);
  Wire.endTransmission();
  Wire.beginTransmission(MPU_ADDR);
  Wire.requestFrom(MPU_ADDR,1);
  byte aByte = Wire.read();

  if(aByte == 0x68){
    Serial.println("Found MPU6050");
  } else {
    Serial.println("Didn't find MPU6050");
  }
}

void processQuat(){
    processed_packet[0] = received_packet[0];
    processed_packet[1] = received_packet[1];
    processed_packet[2] = received_packet[4];
    processed_packet[3] = received_packet[5];
    processed_packet[4] = received_packet[8];
    processed_packet[5] = received_packet[9];
    processed_packet[6] = received_packet[12];
    processed_packet[7] = received_packet[13];   
}
  
void sendQuat(){
  // following conversion adapted from Invensense's TeaPot example
  q[0] = (long) ((((unsigned long) processed_packet[0]) << 8) + ((unsigned long) processed_packet[1]));
  q[1] = (long) ((((unsigned long) processed_packet[2]) << 8) + ((unsigned long) processed_packet[3]));
  q[2] = (long) ((((unsigned long) processed_packet[4]) << 8) + ((unsigned long) processed_packet[5]));
  q[3] = (long) ((((unsigned long) processed_packet[6]) << 8)  + ((unsigned long) processed_packet[7]));
  for(int i = 0; i < 4; i++ ) {
    if( q[i] > 32767 ) {
      q[i] -= 65536;
    }
    q[i] = ((float) q[i]) / 16384.0f;
  }

  char buffer[20] = "";
  serializeQuatData(q, 4, buffer);

  if (connected) {
    // make and send data packet
    ndn_NameComponent dataNameComponents[2];
    DataLite data(dataNameComponents, sizeof(dataNameComponents) / sizeof(dataNameComponents[0]), 0, 0);
    data.getName().append("gyro1");
    char sequence[20] = "";
    sprintf(sequence, "%u", currentIdx);
    data.getName().append(sequence);
    data.setContent(BlobLite((const uint8_t*)buffer, strlen(buffer)));
    signAndSendData(data);
    currentIdx++;    
  }
}

void sendPacket(){
  for(byte i = 0; i < fifoCountL-1; i++){
    Serial.print(received_packet[i], HEX); Serial.print(" ");
  }
  Serial.println(received_packet[fifoCountL-1], HEX); Serial.println();
}

void sendHeader(){
  for(byte i = 0; i < 2; i++){
    Serial.print(received_packet[i], HEX); Serial.print(" ");
  }
  Serial.println();
}

void bank_sel(byte bank){
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6D);
  Wire.write(bank);
  Wire.endTransmission();
}

void serializeQuatData(float * arr, int length, char * result) {
  float euler[3]; 
  quaternionToEuler(arr, euler);
    
  for(int i=0; i<3; i++) {
    //serialFloatPrint(arr[i]);
    Serial.print(euler[i]);
    Serial.print(",");
  }
  Serial.print("\n");
  // for this to work, we need to enable "-u _printf_float" in ~/Library/Arduino15/packages/RFduino/hardware/RFduino/2.3.2/platform.txt
  // per the thread here: http://forum.rfduino.com/index.php?topic=782.0
  sprintf(result, "%.2f,%.2f,%.2f", euler[0], euler[1], euler[2]);
}


void serialFloatPrint(float f) {
  byte * b = (byte *) &f;
  for(int i=0; i<4; i++) {
    
    byte b1 = (b[i] >> 4) & 0x0f;
    byte b2 = (b[i] & 0x0f);
    
    char c1 = (b1 < 10) ? ('0' + b1) : 'A' + b1 - 10;
    char c2 = (b2 < 10) ? ('0' + b2) : 'A' + b2 - 10;
    
    Serial.print(c1);
    Serial.print(c2);
  }
  
}

void quaternionToEuler(float* q, float* euler) {
  euler[0] = atan2(2 * q[1] * q[2] - 2 * q[0] * q[3], 2 * q[0]*q[0] + 2 * q[1] * q[1] - 1); // psi
  euler[1] = -asin(2 * q[1] * q[3] + 2 * q[0] * q[2]); // theta
  euler[2] = atan2(2 * q[2] * q[3] - 2 * q[0] * q[1], 2 * q[0] * q[0] + 2 * q[3] * q[3] - 1); // phi
}
