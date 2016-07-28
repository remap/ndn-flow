/* -*- Mode:C++; c-file-style:"gnu"; indent-tabs-mode:nil -*- */
/**
 * Copyright (C) 2016 Regents of the University of California.
 * @author: Jeff Thompson <jefft0@remap.ucla.edu>
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
#include <ndn-cpp/lite/util/crypto-lite.hpp>
#include "rsaref-crypto.h"
#include "rsaref-rsa-public-key-lite.hpp"

using namespace ndn;

// Define input/output pins.
// Input pins for buttons, and output pins for LED RGB On-Off control
// GPIO2 on the RFduino RGB shield is the Red LED
// GPIO3 on the RFduino RGB shield is the Green LED
// GPIO4 on the RFduino RGB shield is the Blue LED

#define RED_LED_PIN   2
#define GREEN_LED_PIN 3
#define BLUE_LED_PIN  4

uint8_t HmacKey[64];
uint8_t HmacKeyDigest[ndn_SHA256_DIGEST_SIZE];
R_RANDOM_STRUCT RandomStruct;

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
  // put your setup code here, to run once:
  // Enable outputs.
  pinMode(RED_LED_PIN, OUTPUT);
  pinMode(GREEN_LED_PIN, OUTPUT);
  pinMode(BLUE_LED_PIN, OUTPUT);

  // Enable serial debug, type cntrl-M at runtime.
  Serial.begin(9600);
  while (!Serial); // Wait untilSerial is ready.
  Serial.println("RFduino started");

  // Turn Off all LEDs initially
  digitalWrite(RED_LED_PIN, LOW);
  digitalWrite(GREEN_LED_PIN, LOW);
  digitalWrite(BLUE_LED_PIN, LOW);

  // Indicate RGB LED is operational to user.
  digitalWrite(RED_LED_PIN, HIGH);    // red
  delay (500);
  digitalWrite(RED_LED_PIN, LOW);
  digitalWrite(GREEN_LED_PIN, HIGH);  // green
  delay (500);
  digitalWrite(RED_LED_PIN, LOW);
  digitalWrite(GREEN_LED_PIN, LOW);
  digitalWrite(BLUE_LED_PIN, HIGH);   // blue
  delay (500);
  digitalWrite(RED_LED_PIN, LOW);     // lights out
  digitalWrite(GREEN_LED_PIN, LOW);
  digitalWrite(BLUE_LED_PIN, LOW);

  // configure the RFduino BLE properties
  RFduinoBLE.advertisementData = "ledbtn";
  RFduinoBLE.advertisementInterval = 500;
  RFduinoBLE.deviceName = "RFduino";
  RFduinoBLE.txPowerLevel = -20;
  Serial.println("RFduino BLE Advertising interval is 500ms");
  Serial.println("RFduino BLE DeviceName: RFduino");
  Serial.println("RFduino BLE Tx Power Level: -20dBm");

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
}

void
loop()
{
  RFduino_ULPDelay(INFINITE);
}

void
RFduinoBLE_onAdvertisement()
{
  Serial.println("RFduino is doing BLE advertising ...");
  digitalWrite(RED_LED_PIN, LOW);
  digitalWrite(GREEN_LED_PIN, LOW);
  digitalWrite(BLUE_LED_PIN, LOW);
}

void
RFduinoBLE_onConnect()
{
  Serial.println("RFduino BLE connection successful");
  digitalWrite(RED_LED_PIN, LOW);
  digitalWrite(GREEN_LED_PIN, HIGH);
  digitalWrite(BLUE_LED_PIN, LOW);
}

void
RFduinoBLE_onDisconnect()
{
  Serial.println("RFduino BLE disconnected");
  digitalWrite(RED_LED_PIN, LOW);
  digitalWrite(GREEN_LED_PIN, LOW);
  digitalWrite(BLUE_LED_PIN, LOW);
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

      // TODO: send(packet, iFragment + nFragmentBytes);
      
      // Increment the fragment index in the packet.
      ++packet[iFragmentIndex];
  }
}

