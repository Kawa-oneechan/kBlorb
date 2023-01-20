using System;
using System.IO;

namespace kBlorb
{
	/// <summary>
	/// Allows reading integers in big-endian byte order.
	/// </summary>
	static class Motorola
	{
		/// <summary>
		/// Reads a 2-byte signed integer in big-endian order from the current stream and advances the current position of the stream by two bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>A 2-byte signed integer read from the current stream.</returns>
		public static Int16 ReadMotoInt16(this BinaryReader stream)
		{
			var moto2 = stream.ReadBytes(2);
			Array.Reverse(moto2);
			return BitConverter.ToInt16(moto2, 0);
		}

		/// <summary>
		/// Reads a 4-byte signed integer in big-endian order from the current stream and advances the current position of the stream by four bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>A 4-byte signed integer read from the current stream.</returns>
		public static Int32 ReadMotoInt32(this BinaryReader stream)
		{
			var moto4 = stream.ReadBytes(4);
			Array.Reverse(moto4);
			return BitConverter.ToInt32(moto4, 0);
		}

		/// <summary>
		/// Reads an 8-byte signed integer in big-endian order from the current stream and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>An 8-byte signed integer read from the current stream.</returns>
		public static Int64 ReadMotoInt64(this BinaryReader stream)
		{
			var moto8 = stream.ReadBytes(8);
			Array.Reverse(moto8);
			return BitConverter.ToInt64(moto8, 0);
		}

		/// <summary>
		/// Reads a 2-byte unsigned integer in big-endian order from the current stream and advances the current position of the stream by two bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>A 2-byte unsigned integer read from the current stream.</returns>
		public static UInt16 ReadMotoUInt16(this BinaryReader stream)
		{
			var moto2 = stream.ReadBytes(2);
			Array.Reverse(moto2);
			return BitConverter.ToUInt16(moto2, 0);
		}

		/// <summary>
		/// Reads a 4-byte unsigned integer in big-endian order from the current stream and advances the current position of the stream by four bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>A 4-byte unsigned integer read from the current stream.</returns>
		public static UInt32 ReadMotoUInt32(this BinaryReader stream)
		{
			var moto4 = stream.ReadBytes(4);
			Array.Reverse(moto4);
			return BitConverter.ToUInt32(moto4, 0);
		}

		/// <summary>
		/// Reads an 8-byte unsigned integer in big-endian order from the current stream and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>An 8-byte unsigned integer read from the current stream.</returns>
		public static UInt64 ReadMotoUInt64(this BinaryReader stream)
		{
			var moto8 = stream.ReadBytes(8);
			Array.Reverse(moto8);
			return BitConverter.ToUInt64(moto8, 0);
		}

		/// <summary>
		/// Reads an 8-byte floating point value in big-endian order from the current stream and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <returns>An 8-byte floating point value read from the current stream.</returns>
		public static double ReadDouble(this BinaryReader stream)
		{
			var moto8 = stream.ReadBytes(8);
			Array.Reverse(moto8);
			return BitConverter.ToDouble(moto8, 0);
		}

		/// <summary>
		/// Writes a 2-byte signed integer to the current stream in big-endian order and advances the current position of the stream by two bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, Int16 value)
		{
			var moto2 = BitConverter.GetBytes(value);
			Array.Reverse(moto2);
			stream.Write(moto2);
		}

		/// <summary>
		/// Writes a 4-byte signed integer to the current stream in big-endian order and advances the current position of the stream by four bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, Int32 value)
		{
			var moto4 = BitConverter.GetBytes(value);
			Array.Reverse(moto4);
			stream.Write(moto4);
		}

		/// <summary>
		/// Writes an 8-byte signed integer in big-endian order to the current stream and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, Int64 value)
		{
			var moto8 = BitConverter.GetBytes(value);
			Array.Reverse(moto8);
			stream.Write(moto8);
		}

		/// <summary>
		/// Writes a 2-byte unsigned integer to the current stream in big-endian order and advances the current position of the stream by two bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, UInt16 value)
		{
			var moto2 = BitConverter.GetBytes(value);
			Array.Reverse(moto2);
			stream.Write(moto2);
		}

		/// <summary>
		/// Writes a 4-byte unsigned integer to the current stream in big-endian order and advances the current position of the stream by four bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, UInt32 value)
		{
			var moto4 = BitConverter.GetBytes(value);
			Array.Reverse(moto4);
			stream.Write(moto4);
		}

		/// <summary>
		/// Writes an 8-byte unsigned integer to the current stream in big-endian order and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, UInt64 value)
		{
			var moto8 = BitConverter.GetBytes(value);
			Array.Reverse(moto8);
			stream.Write(moto8);
		}

		/// <summary>
		/// Writes an 8-byte floating-point value to the current stream in big-endian order and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <param name="stream">A stream.</param>
		/// <param name="value">The value to write.</param>
		public static void WriteMoto(this BinaryWriter stream, double value)
		{
			var moto8 = BitConverter.GetBytes(value);
			Array.Reverse(moto8);
			stream.Write(moto8);
		}
	}

}
