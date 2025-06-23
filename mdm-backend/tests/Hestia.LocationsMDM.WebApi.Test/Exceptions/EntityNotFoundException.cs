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
    public class EntityNotFoundExceptionTest
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
        /// <para>Tests <see cref="EntityNotFoundException()"/> constructor and inheritance.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtor()
        {
            var instance = new EntityNotFoundException();
            Assert.AreEqual(typeof(PersistenceException), instance.GetType().BaseType,
                "The class should inherit from $(base_type).");
        }

        /// <summary>
        /// <para>Tests <see cref="EntityNotFoundException(string)"/> constructor
        /// by passing a null reference.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageNull()
        {
            new EntityNotFoundException(null);
        }

        /// <summary>
        /// <para>Tests <see cref="EntityNotFoundException(string)"/> constructor
        /// by passing an error message.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageValid()
        {
            Exception e = new EntityNotFoundException(message);
            Assert.AreEqual(message, e.Message, "e.Message should be equal to message.");
        }

        /// <summary>
        /// <para>Tests <see cref="EntityNotFoundException(string, Exception)"/> constructor
        /// by passing null references.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerNull1()
        {
            new EntityNotFoundException(null, null);
        }

        /// <summary>
        /// <para>Test <see cref="EntityNotFoundException(string, Exception)"/> constructor
        /// by passing an error message and a null reference.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerNull2()
        {
            Exception e = new EntityNotFoundException(message, null);
            Assert.AreEqual(message, e.Message, "e.Message should be equal to message.");
        }

        /// <summary>
        /// <para>Test <see cref="EntityNotFoundException(string, Exception)"/> constructor
        /// by passing a null reference and an inner exception.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerNull3()
        {
            Exception e = new EntityNotFoundException(null, cause);
            Assert.AreEqual(cause, e.InnerException, "e.InnerException should be equal to cause.");
        }

        /// <summary>
        /// <para>Tests <see cref="EntityNotFoundException(string, Exception)"/> constructor
        /// by passing an error message and an inner exception.</para>
        ///
        /// <para>Should be correct.</para>
        /// </summary>
        [TestMethod]
        public void TestCtorMessageInnerValid()
        {
            Exception e = new EntityNotFoundException(message, cause);
            Assert.AreEqual(message, e.Message, "e.Message should be equal to message.");
            Assert.AreEqual(cause, e.InnerException, "e.InnerException should be equal to cause.");
        }

        /// <summary>
        /// <para>Tests <see cref="EntityNotFoundException(SerializationInfo, StreamingContext)"/> constructor.</para>
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
                EntityNotFoundException serial = new EntityNotFoundException(message, cause);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, serial);

                // deserialize the instance
                stream.Seek(0, SeekOrigin.Begin);
                EntityNotFoundException deserial =
                    formatter.Deserialize(stream) as EntityNotFoundException;

                // verify the instance
                Assert.IsFalse(serial == deserial, "Instance not deserialized.");
                Assert.AreEqual(serial.Message, deserial.Message, "Message mismatches.");
                Assert.AreEqual(serial.InnerException.Message, deserial.InnerException.Message,
                    "InnerException mismatches.");
            }
        }
    }
}
