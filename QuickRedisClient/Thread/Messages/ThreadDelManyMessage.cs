﻿using QuickRedisClient.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace QuickRedisClient.Thread.Messages {
	/// <summary>
	/// Send: [DEL key...]
	/// Recv: Integer reply
	/// </summary>
	internal static class ThreadDelManyMessage {
		private readonly static byte[] MessageBulkString =
			ObjectCache.AsciiEncoding.GetBytes("$3\r\nDEL\r\n");

		public static void Send(Socket client, byte[] sendbuf, IList<RedisObject> keys) {
			int len = 0;
			BlockingBufferWriter.WriteArrayHeaderOnly(sendbuf, ref len, keys.Count + 1);
			BlockingBufferWriter.WriteRawString(sendbuf, ref len, MessageBulkString);
			foreach (var key in keys) {
				var keyBytes = (byte[])key;
				if (len + keys.Count +
					BlockingBufferWriter.MaxBulkStringAdditionalLength > sendbuf.Length) {
					BlockingBufferWriter.FlushSendBuf(client, sendbuf, ref len);
				}
				if (keys.Count > SmallStringOptimization.SendInsteadOfCopyIfGT) {
					BlockingBufferWriter.WriteBulkStringHeaderOnly(sendbuf, ref len, keyBytes);
					BlockingBufferWriter.FlushSendBuf(client, sendbuf, ref len);
					BlockingBufferWriter.FlushSendBuf(client, keyBytes);
					BlockingBufferWriter.FlushSendBuf(client, ObjectCache.CRLF);
				} else {
					BlockingBufferWriter.WriteBulkString(sendbuf, ref len, keyBytes);
				}
			}
			BlockingBufferWriter.FlushSendBuf(client, sendbuf, ref len);
		}

		public static long Recv(Socket client, byte[] recvbuf, ref int start, ref int end) {
			var result = BlockingBufferReader.ReadRESP(client, recvbuf, ref start, ref end);
			if (result is long) {
				return (long)result;
			} else if (result is Exception) {
				throw (Exception)result;
			} else {
				throw new RedisClientException($"Redis client error: Unknow result from DEL response: {result}");
			}
		}
	}
}
