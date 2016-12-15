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

#ifndef NDN_RSAREF_RSA_PUBLIC_KEY_H
#define NDN_RSAREF_RSA_PUBLIC_KEY_H

#include <ndn-cpp/c/common.h>
#include <ndn-cpp/c/errors.h>
#include <ndn-cpp/c/encrypt/algo/encrypt-params-types.h>
#if 0 // TODO: Move header files to the proper location.
#include <ndn-cpp/c/security/rsaref-rsa-public-key-types.h>
#else
#include "rsaref-rsa-public-key-types.h"
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Initialize the ndn_RsarefRsaPublicKey struct to use the given publicKey.
 * @param self A pointer to the ndn_RsarefRsaPublicKey struct.
 * @param publicKey The RSAREF R_RSA_PUBLIC_KEY struct which is already
 * initialized.
 */
static __inline void
ndn_RsarefRsaPublicKey_initialize
  (struct ndn_RsarefRsaPublicKey *self, R_RSA_PUBLIC_KEY *publicKey)
{
  self->publicKey = publicKey;
}

/**
 * Use this public key to encrypt plainData according to the algorithmType.
 * @param self A pointer to the ndn_RsarefRsaPublicKey struct.
 * @param plainData A pointer to the input byte array to encrypt.
 * @param plainDataLength The length of plainData.
 * @param algorithmType This encrypts according to algorithmType.
 * @param encryptedData A pointer to the encrypted output buffer. The caller
 * must provide a buffer large enough to receive the bytes.
 * @param encryptedDataLength Set encryptedDataLength to the number of bytes
 * placed in the encryptedData buffer.
 * @return 0 for success, else NDN_ERROR_Unsupported_algorithm_type for
 * unsupported algorithmType padding scheme, or
 * NDN_ERROR_Error_in_encrypt_operation if can't complete the encrypt 
 * operation, or NDN_ERROR_Error_in_generate_operation is the RSAREF
 * global random struct is not seeded.
 */
ndn_Error
ndn_RsarefRsaPublicKey_encrypt
  (const struct ndn_RsarefRsaPublicKey *self, const uint8_t *plainData,
   size_t plainDataLength, ndn_EncryptAlgorithmType algorithmType,
   uint8_t *encryptedData, size_t *encryptedDataLength);

#ifdef __cplusplus
}
#endif

#endif
