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

#ifndef NDN_RSAREF_CRYPTO_H
#define NDN_RSAREF_CRYPTO_H

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Get the number of remaining bytes needed to seed the global RSAREF random
 * number generator.
 * @return The number of bytes needed. If 0, then the random number generator is
 * ready. Otherwise, call ndn_RsarefCrypto_randomUpdate to update with seed bytes.
 */
unsigned int
ndn_RsarefCrypto_getRandomBytesNeeded();

/**
 * Update the global RSAREF random number generator with the given seed bytes.
 * @param buffer The buffer with the seed bytes.
 * @param bufferLength The number of bytes in buffer.
 */
void
ndn_RsarefCrypto_randomUpdate(const uint8_t *buffer, size_t bufferLength);

#ifdef __cplusplus
}
#endif

#endif
