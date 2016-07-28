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

#ifndef NDN_RSAREF_RSA_PUBLIC_KEY_LITE_HPP
#define NDN_RSAREF_RSA_PUBLIC_KEY_LITE_HPP

#if 0 // TODO: Move header files to the proper location.
#include "../util/blob-lite.hpp"
#include "../../c/errors.h"
#include "../../c/encrypt/algo/encrypt-params-types.h"
#include "../../c/security/rsaref-rsa-public-key-types.h"
#else
#include <ndn-cpp/lite/util/blob-lite.hpp>
#include <ndn-cpp/c/errors.h>
#include <ndn-cpp/c/encrypt/algo/encrypt-params-types.h>
#include "rsaref-rsa-public-key-types.h"
#endif

namespace ndn {

/**
 * An RsarefRsaPublicKeyLite holds a pointer to an R_RSA_PUBLIC_KEY for use in 
 * crypto operations based on RSAREF.
 * This imitates RsaPublicKeyLite.
 */
class RsarefRsaPublicKeyLite : private ndn_RsarefRsaPublicKey {
public:
  /**
   * Create an RsarefRsaPublicKeyLite to use the given publicKey.
   * @param publicKey The RSAREF R_RSA_PUBLIC_KEY struct which is already
   * initialized.
   */
  RsarefRsaPublicKeyLite(R_RSA_PUBLIC_KEY& publicKey);
    
  /**
   * Use this public key to encrypt plainData according to the algorithmType.
   * @param plainData A pointer to the input byte array to encrypt.
   * @param plainDataLength The length of plainData.
   * @param algorithmType This encrypts according to algorithmType.
   * @param encryptedData A pointer to the encrypted output buffer. The caller
   * must provide a buffer large enough to receive the bytes.
   * @param encryptedDataLength Set encryptedDataLength to the number of bytes
   * placed in the encryptedData buffer.
   * @param randomStruct An R_RANDOM_STRUCT which must already be initialized
   * and seeded so that R_GetRandomBytesNeeded sets bytesNeeded to 0.
   * @return 0 for success, else NDN_ERROR_Unsupported_algorithm_type for
   * unsupported algorithmType padding scheme, or
   * NDN_ERROR_Error_in_encrypt_operation if can't complete the encrypt 
   * operation, including if randomStruct is not seeded..
   */
  ndn_Error
  encrypt
    (const uint8_t* plainData, size_t plainDataLength,
     ndn_EncryptAlgorithmType algorithmType, uint8_t* encryptedData,
     size_t& encryptedDataLength, R_RANDOM_STRUCT& randomStruct) const;

  /**
   * Use this public key to encrypt plainData according to the algorithmType.
   * @param plainData The input byte array to encrypt.
   * @param algorithmType This encrypts according to algorithmType.
   * @param encryptedData A pointer to the encrypted output buffer. The caller
   * must provide a buffer large enough to receive the bytes.
   * @param encryptedDataLength Set encryptedDataLength to the number of bytes
   * placed in the encryptedData buffer.
   * @param randomStruct An R_RANDOM_STRUCT which must already be initialized
   * and seeded so that R_GetRandomBytesNeeded sets bytesNeeded to 0.
   * @return 0 for success, else NDN_ERROR_Unsupported_algorithm_type for
   * unsupported algorithmType padding scheme, or
   * NDN_ERROR_Error_in_encrypt_operation if can't complete the encrypt
   * operation, including if randomStruct is not seeded..
   */
  ndn_Error
  encrypt
    (const BlobLite& plainData, ndn_EncryptAlgorithmType algorithmType,
     uint8_t* encryptedData, size_t& encryptedDataLength,
     R_RANDOM_STRUCT& randomStruct) const
  {
    return encrypt
      (plainData.buf(), plainData.size(), algorithmType, encryptedData,
       encryptedDataLength, randomStruct);
  }

  /**
   * Downcast the reference to the ndn_RsarefRsaPublicKey struct to an
   * RsarefRsaPublicKeyLite.
   * @param blob A reference to the ndn_RsarefRsaPublicKey struct.
   * @return The same reference as RsarefRsaPublicKeyLite.
   */
  static RsarefRsaPublicKeyLite&
  downCast(ndn_RsarefRsaPublicKey& blob) { return *(RsarefRsaPublicKeyLite*)&blob; }

  static const RsarefRsaPublicKeyLite&
  downCast(const ndn_RsarefRsaPublicKey& blob) { return *(RsarefRsaPublicKeyLite*)&blob; }
};

}

#endif
