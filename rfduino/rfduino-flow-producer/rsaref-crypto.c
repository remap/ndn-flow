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

#include <ndn-cpp/c/common.h>
#include <ndn-cpp/c/errors.h>
#if 0 // TODO: Move header files to the proper location.
#include "../../../contrib/rsaref/source/global.h"
#include "../../../contrib/rsaref/source/rsaref.h"
#include "../../../contrib/rsaref/source/r_random.h"
#else
#include <rsaref/source/global.h>
#include <rsaref/source/rsaref.h>
int R_GenerateBytes(unsigned char *, unsigned int, R_RANDOM_STRUCT *);
#endif
#include "rsaref-crypto.h"

static R_RANDOM_STRUCT GlobalRandomStruct;
static int GlobalRandomStructIsInitialized = 0;

R_RANDOM_STRUCT *ndn_RsarefCrypto_getGlobalRandomStruct()
{
  if (!GlobalRandomStructIsInitialized) {
    R_RandomInit(&GlobalRandomStruct);
    GlobalRandomStructIsInitialized = 1;
  }

  return &GlobalRandomStruct;
}

unsigned int
ndn_RsarefCrypto_getRandomBytesNeeded()
{
  unsigned int bytesNeeded;
  R_GetRandomBytesNeeded(&bytesNeeded, ndn_RsarefCrypto_getGlobalRandomStruct());
  return bytesNeeded;
}

void
ndn_RsarefCrypto_randomUpdate(const uint8_t *buffer, size_t bufferLength)
{
  R_RandomUpdate
    (ndn_RsarefCrypto_getGlobalRandomStruct(), (unsigned char *)buffer,
     bufferLength);
}

// Supply the definition of the function declared in crypto.h
ndn_Error
ndn_generateRandomBytes(uint8_t *buffer, size_t bufferLength)
{
  if (R_GenerateBytes
      ((unsigned char *)buffer, bufferLength, 
       ndn_RsarefCrypto_getGlobalRandomStruct()) != 0)
    return NDN_ERROR_Error_in_generate_operation;

  return NDN_ERROR_success;
}
