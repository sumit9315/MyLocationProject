/*
 * Copyright (c) 2020, TopCoder, Inc. All rights reserved.
 */
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class AuthorizationExceptionTest
    {
        /// <summary>
        /// Message string for test.
        /// </summary>
        private const string message = "message";

        /// <summary>
        /// Exception instance for test.
        /// </summary>
        private Exception cause = new Exception("innerException");

        /// <summary>
        /// <para>Tests <see cref="AuthorizationException()"/> constructor and inheritance.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtor()
        {
            var instance = new AuthorizationException();
            Assert.AreEqual(typeof(ServiceException), instance.GetType().BaseType,
                "The class should inherit from $(base_type).");
        }

        /// <summary>
        /// <para>Tests <see cref="AuthorizationException(string)"/> constructor
        /// by passing a null reference.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageNull()
        {
            new AuthorizationException(null);
        }

        /// <summary>
        /// <para>Tests <see cref="AuthorizationException(string)"/> constructor
        /// by passing an error message.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageValid()
        {
            Exception e = new AuthorizationException(message);
            Assert.AreEqual(message, e.Message, "e.Message should be equal to message.");
        }

        /// <summary>
        /// <para>Tests <see cref="AuthorizationException(string, Exception)"/> constructor
        /// by passing null references.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerNull1()
        {
            new AuthorizationException(null, null);
        }

        /// <summary>
        /// <para>Test <see cref="AuthorizationException(string, Exception)"/> constructor
        /// by passing an error message and a null reference.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerNull2()
        {
            Exception e = new AuthorizationException(message, null);
            Assert.AreEqual(message, e.Message, "e.Message should be equal to message.");
        }

        /// <summary>
        /// <para>Test <see cref="AuthorizationException(string, Exception)"/> constructor
        /// by passing a null reference and an inner exception.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerNull3()
        {
            Exception e = new AuthorizationException(null, cause);
            Assert.AreEqual(cause, e.InnerException, "e.InnerException should be equal to cause.");
        }

        /// <summary>
        /// <para>Tests <see cref="AuthorizationException(string, Exception)"/> constructor
        /// by passing an error message and an inner exception.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerValid()
        {
            Exception e = new AuthorizationException(message, cause);
            Assert.AreEqual(message, e.Message, "e.Message should be equal to message.");
            Assert.AreEqual(cause, e.InnerException, "e.InnerException should be equal to cause.");
        }

        /// <summary>
        /// <para>Tests <see cref="AuthorizationException(SerializationInfo, StreamingContext)"/> constructor.</para>
        ///
        /// <para>Deserialized instance should have the same property value as it was before serialization.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorInfoContext()
        {
            // Stream for serialization.
            using (Stream stream = new MemoryStream())
            {
                // serialize the instance
                AuthorizationException serial = new AuthorizationException(message, cause);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, serial);

                // deserialize the instance
                stream.Seek(0, SeekOrigin.Begin);
                AuthorizationException deserial =
                    formatter.Deserialize(stream) as AuthorizationException;

                // verify the instance
                Assert.IsFalse(serial == deserial, "Instance not deserialized.");
                Assert.AreEqual(serial.Message, deserial.Message, "Message mismatches.");
                Assert.AreEqual(serial.InnerException.Message, deserial.InnerException.Message,
                    "InnerException mismatches.");
            }
        }
    }
}
