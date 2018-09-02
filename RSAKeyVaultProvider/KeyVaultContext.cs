﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.WebKey;

namespace Microsoft.Azure.KeyVault
{
    /// <summary>
    /// A signing context used for signing packages with Azure Key Vault Keys.
    /// </summary>
    public struct KeyVaultContext
    {
        readonly KeyVaultClient client;

        /// <summary>
        /// Creates a new Key Vault context.
        /// </summary>
        public KeyVaultContext(KeyVaultClient client, KeyIdentifier keyIdentifier, JsonWebKey key)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            KeyIdentifier = keyIdentifier ?? throw new ArgumentNullException(nameof(keyIdentifier));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Certificate = null;
        }

        /// <summary>
        /// Creates a new Key Vault context.
        /// </summary>
        public KeyVaultContext(KeyVaultClient client, KeyIdentifier keyIdentifier, X509Certificate2 publicCertificate)
        {
            Certificate = publicCertificate ?? throw new ArgumentNullException(nameof(publicCertificate));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            KeyIdentifier = keyIdentifier ?? throw new ArgumentNullException(nameof(keyIdentifier));
            using (var rsa = publicCertificate.GetRSAPublicKey())
            {
                Key = new JsonWebKey(rsa.ExportParameters(false));
            }
        }

        /// <summary>
        /// Gets the certificate and public key used to validate the signature. May be null if 
        /// Key isn't part of a certificate
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Identifyer of current key
        /// </summary>
        public KeyIdentifier KeyIdentifier { get; }

        /// <summary>
        /// Public key 
        /// </summary>
        public JsonWebKey Key { get; }

        internal async Task<byte[]> SignDigestAsync(byte[] digest, HashAlgorithmName hashAlgorithm)
        {
            var algorithm = SignatureAlgorithmTranslator.SignatureAlgorithmToJwsAlgId(hashAlgorithm);

            if (hashAlgorithm == HashAlgorithmName.SHA1)
                digest = Sha1Helper.CreateDigest(digest);

            var signature = await client.SignAsync(KeyIdentifier.Identifier, algorithm, digest).ConfigureAwait(false);
            return signature.Result;
        }

        internal async Task<byte[]> DecryptDataAsync(byte[] cipherText, RSAEncryptionPadding padding)
        {
            var algorithm = EncryptionPaddingTranslator.EncryptionPaddingToJwsAlgId(padding);
            var data = await client.DecryptAsync(KeyIdentifier.Identifier, algorithm, cipherText).ConfigureAwait(false);
            return data.Result;
        }

        /// <summary>
        /// Returns true if properly constructed. If default, then false.
        /// </summary>
        public bool IsValid => client != null;
    }
}
