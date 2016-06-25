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
#include <ndn-cpp/lite/encoding/tlv-0_1_1-wire-format-lite.hpp>
#include <ndn-cpp/lite/util/crypto-lite.hpp>

using namespace ndn;

// Define input/output pins.
// Input pins for buttons, and output pins for LED RGB On-Off control
// GPIO2 on the RFduino RGB shield is the Red LED
// GPIO3 on the RFduino RGB shield is the Green LED
// GPIO4 on the RFduino RGB shield is the Blue LED

#define RED_LED_PIN   2
#define GREEN_LED_PIN 3
#define BLUE_LED_PIN  4

uint8_t HMAC_KEY[64];
uint8_t HMAC_KEY_DIGEST[ndn_SHA256_DIGEST_SIZE];

static void
printHex(const uint8_t* data, size_t dataLength)
{
  for (size_t i = 0; i < dataLength; ++i) {
    char buf[5];
    sprintf(buf, " %02x", (int)data[i]);
    Serial.print(buf);
  }
}

void
setup()
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

  // Generate two bytes of random seed.
  // Turn on the random number generator, clear the ready flag and wait for a value.
  NRF_RNG->TASKS_START = 1;
  NRF_RNG->EVENTS_VALRDY = 0;
  while (NRF_RNG->EVENTS_VALRDY == 0);
  int seed = NRF_RNG->VALUE;
  NRF_RNG->EVENTS_VALRDY = 0;
  while (NRF_RNG->EVENTS_VALRDY == 0);
  seed *= NRF_RNG->VALUE;
  randomSeed(seed);
  // Turn on the random number generator since it doesn't work when BLE is enabled.
  NRF_RNG->TASKS_STOP = 1;
  
  // Generate the HMAC_KEY.
  for (size_t i = 0; i < sizeof(HMAC_KEY); ++i)
    HMAC_KEY[i] = random(0, 256);
  // Set HMAC_KEY_DIGEST to sha256(HMAC_KEY).
  CryptoLite::digestSha256(HMAC_KEY, sizeof(HMAC_KEY), HMAC_KEY_DIGEST);

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

// TODO: Determine the correct max size of a received packet.
uint8_t receiveBuffer[100];
size_t receiveBufferLength = 0;
int expectedFragmentIndex = 0;

void
RFduinoBLE_onReceive(char *data, int dataLength)
{
  Serial.print("Debug: Received data over BLE:");
  printHex((const uint8_t*)data, dataLength);
  Serial.println("");
  
  // Defragment into the receiveBuffer.
  const size_t iFragmentIndex = 0;
  const size_t iFragmentCount = 1;
  const size_t iFragment = 2;
  if (dataLength < iFragment + 1)
    // We don't even have one fragment byte.
    return;
  int fragmentIndex = data[iFragmentIndex];
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

  size_t nFragmentBytes = dataLength - iFragment;
  if (receiveBufferLength + nFragmentBytes > sizeof(receiveBuffer)) {
    Serial.println("Error: Exceeded sizeof(receiveBuffer)");
    return;
  }
  memcpy(receiveBuffer + receiveBufferLength, data + iFragment, nFragmentBytes);
  receiveBufferLength += nFragmentBytes;
  
  expectedFragmentIndex = fragmentIndex + 1;
  if (expectedFragmentIndex >= data[iFragmentCount]) {
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
    if ((error = Tlv0_1_1WireFormatLite::decodeInterest
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

