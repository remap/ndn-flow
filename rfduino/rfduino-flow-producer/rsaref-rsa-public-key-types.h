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

#ifndef NDN_RSAREF_RSA_PUBLIC_KEY_TYPES_H
#define NDN_RSAREF_RSA_PUBLIC_KEY_TYPES_H

#if 1 // TODO: Update the R_RSA_PUBLIC_KEY definition so we can forward-declare
      // it and not include RSAREF headers.
#include <rsaref/source/global.h>
#include <rsaref/source/rsaref.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * A struct ndn_RsarefRsaPublicKey holds a pointer to an R_RSA_PUBLIC_KEY for
 * use in  crypto operations based on RSAREF.
 * This imitates struct ndn_RsaPublicKey.
 */
struct ndn_RsarefRsaPublicKey {
  R_RSA_PUBLIC_KEY *publicKey;
};

#ifdef __cplusplus
}
#endif

#endif
