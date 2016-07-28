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

#if 0 // TODO: Move header files to the proper location.
#include "../../c/security/rsaref-rsa-public-key.h"
#include <ndn-cpp/lite/security/rsaref-rsa-public-key-lite.hpp>
#else
#include "rsaref-rsa-public-key.h"
#include "rsaref-rsa-public-key-lite.hpp"
#endif

namespace ndn {

RsarefRsaPublicKeyLite::RsarefRsaPublicKeyLite(R_RSA_PUBLIC_KEY& publicKey)
{
  ndn_RsarefRsaPublicKey_initialize(this, &publicKey);
}

ndn_Error
RsarefRsaPublicKeyLite::encrypt
  (const uint8_t* plainData, size_t plainDataLength,
   ndn_EncryptAlgorithmType algorithmType, uint8_t* encryptedData,
   size_t& encryptedDataLength) const
{
  return ndn_RsarefRsaPublicKey_encrypt
    (this, plainData, plainDataLength, algorithmType, encryptedData,
     &encryptedDataLength);
}

}
