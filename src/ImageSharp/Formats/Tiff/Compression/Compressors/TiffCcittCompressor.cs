// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp.Memory;

namespace SixLabors.ImageSharp.Formats.Tiff.Compression.Compressors
{
    /// <summary>
    /// Common functionality for CCITT T4 and T6 Compression
    /// </summary>
    internal abstract class TiffCcittCompressor : TiffBaseCompressor
    {
        protected const uint WhiteZeroRunTermCode = 0x35;

        protected const uint BlackZeroRunTermCode = 0x37;

        private static readonly uint[] MakeupRunLength =
        {
            64, 128, 192, 256, 320, 384, 448, 512, 576, 640, 704, 768, 832, 896, 960, 1024, 1088, 1152, 1216, 1280, 1344, 1408, 1472, 1536, 1600, 1664, 1728, 1792, 1856, 1920, 1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560
        };

        private static readonly Dictionary<uint, uint> WhiteLen4TermCodes = new Dictionary<uint, uint>()
        {
            { 2, 0x7 }, { 3, 0x8 }, { 4, 0xB }, { 5, 0xC }, { 6, 0xE }, { 7, 0xF }
        };

        private static readonly Dictionary<uint, uint> WhiteLen5TermCodes = new Dictionary<uint, uint>()
        {
            { 8, 0x13 }, { 9, 0x14 }, { 10, 0x7 }, { 11, 0x8 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen6TermCodes = new Dictionary<uint, uint>()
        {
            { 1, 0x7 }, { 12, 0x8 }, { 13, 0x3 }, { 14, 0x34 }, { 15, 0x35 }, { 16, 0x2A }, { 17, 0x2B }
        };

        private static readonly Dictionary<uint, uint> WhiteLen7TermCodes = new Dictionary<uint, uint>()
        {
            { 18, 0x27 }, { 19, 0xC }, { 20, 0x8 }, { 21, 0x17 }, { 22, 0x3 }, { 23, 0x4 }, { 24, 0x28 }, { 25, 0x2B }, { 26, 0x13 },
            { 27, 0x24 }, { 28, 0x18 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen8TermCodes = new Dictionary<uint, uint>()
        {
            { 0, WhiteZeroRunTermCode }, { 29, 0x2 }, { 30, 0x3 }, { 31, 0x1A }, { 32, 0x1B }, { 33, 0x12 }, { 34, 0x13 }, { 35, 0x14 },
            { 36, 0x15 }, { 37, 0x16 }, { 38, 0x17 }, { 39, 0x28 }, { 40, 0x29 }, { 41, 0x2A }, { 42, 0x2B }, { 43, 0x2C }, { 44, 0x2D },
            { 45, 0x4 }, { 46, 0x5 }, { 47, 0xA }, { 48, 0xB }, { 49, 0x52 }, { 50, 0x53 }, { 51, 0x54 }, { 52, 0x55 }, { 53, 0x24 },
            { 54, 0x25 }, { 55, 0x58 }, { 56, 0x59 }, { 57, 0x5A }, { 58, 0x5B }, { 59, 0x4A }, { 60, 0x4B }, { 61, 0x32 }, { 62, 0x33 },
            { 63, 0x34 }
        };

        private static readonly Dictionary<uint, uint> BlackLen2TermCodes = new Dictionary<uint, uint>()
        {
            { 2, 0x3 }, { 3, 0x2 }
        };

        private static readonly Dictionary<uint, uint> BlackLen3TermCodes = new Dictionary<uint, uint>()
        {
            { 1, 0x2 }, { 4, 0x3 }
        };

        private static readonly Dictionary<uint, uint> BlackLen4TermCodes = new Dictionary<uint, uint>()
        {
            { 5, 0x3 }, { 6, 0x2 }
        };

        private static readonly Dictionary<uint, uint> BlackLen5TermCodes = new Dictionary<uint, uint>()
        {
            { 7, 0x3 }
        };

        private static readonly Dictionary<uint, uint> BlackLen6TermCodes = new Dictionary<uint, uint>()
        {
            { 8, 0x5 }, { 9, 0x4 }
        };

        private static readonly Dictionary<uint, uint> BlackLen7TermCodes = new Dictionary<uint, uint>()
        {
            { 10, 0x4 }, { 11, 0x5 }, { 12, 0x7 }
        };

        private static readonly Dictionary<uint, uint> BlackLen8TermCodes = new Dictionary<uint, uint>()
        {
            { 13, 0x4 }, { 14, 0x7 }
        };

        private static readonly Dictionary<uint, uint> BlackLen9TermCodes = new Dictionary<uint, uint>()
        {
            { 15, 0x18 }
        };

        private static readonly Dictionary<uint, uint> BlackLen10TermCodes = new Dictionary<uint, uint>()
        {
            { 0, BlackZeroRunTermCode }, { 16, 0x17 }, { 17, 0x18 }, { 18, 0x8 }
        };

        private static readonly Dictionary<uint, uint> BlackLen11TermCodes = new Dictionary<uint, uint>()
        {
            { 19, 0x67 }, { 20, 0x68 }, { 21, 0x6C }, { 22, 0x37 }, { 23, 0x28 }, { 24, 0x17 }, { 25, 0x18 }
        };

        private static readonly Dictionary<uint, uint> BlackLen12TermCodes = new Dictionary<uint, uint>()
        {
            { 26, 0xCA }, { 27, 0xCB }, { 28, 0xCC }, { 29, 0xCD }, { 30, 0x68 }, { 31, 0x69 }, { 32, 0x6A }, { 33, 0x6B }, { 34, 0xD2 },
            { 35, 0xD3 }, { 36, 0xD4 }, { 37, 0xD5 }, { 38, 0xD6 }, { 39, 0xD7 }, { 40, 0x6C }, { 41, 0x6D }, { 42, 0xDA }, { 43, 0xDB },
            { 44, 0x54 }, { 45, 0x55 }, { 46, 0x56 }, { 47, 0x57 }, { 48, 0x64 }, { 49, 0x65 }, { 50, 0x52 }, { 51, 0x53 }, { 52, 0x24 },
            { 53, 0x37 }, { 54, 0x38 }, { 55, 0x27 }, { 56, 0x28 }, { 57, 0x58 }, { 58, 0x59 }, { 59, 0x2B }, { 60, 0x2C }, { 61, 0x5A },
            { 62, 0x66 }, { 63, 0x67 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen5MakeupCodes = new Dictionary<uint, uint>()
        {
            { 64, 0x1B }, { 128, 0x12 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen6MakeupCodes = new Dictionary<uint, uint>()
        {
            { 192, 0x17 }, { 1664, 0x18 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen8MakeupCodes = new Dictionary<uint, uint>()
        {
            { 320, 0x36 }, { 384, 0x37 }, { 448, 0x64 }, { 512, 0x65 }, { 576, 0x68 }, { 640, 0x67 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen7MakeupCodes = new Dictionary<uint, uint>()
        {
            { 256, 0x37 }
        };

        private static readonly Dictionary<uint, uint> WhiteLen9MakeupCodes = new Dictionary<uint, uint>()
        {
            { 704, 0xCC }, { 768, 0xCD }, { 832, 0xD2 }, { 896, 0xD3 }, { 960, 0xD4 }, { 1024, 0xD5 }, { 1088, 0xD6 },
            { 1152, 0xD7 }, { 1216, 0xD8 }, { 1280, 0xD9 }, { 1344, 0xDA }, { 1408, 0xDB }, { 1472, 0x98 }, { 1536, 0x99 },
            { 1600, 0x9A }, { 1728, 0x9B }
        };

        private static readonly Dictionary<uint, uint> WhiteLen11MakeupCodes = new Dictionary<uint, uint>()
        {
            { 1792, 0x8 }, { 1856, 0xC }, { 1920, 0xD }
        };

        private static readonly Dictionary<uint, uint> WhiteLen12MakeupCodes = new Dictionary<uint, uint>()
        {
            { 1984, 0x12 }, { 2048, 0x13 }, { 2112, 0x14 }, { 2176, 0x15 }, { 2240, 0x16 }, { 2304, 0x17 }, { 2368, 0x1C },
            { 2432, 0x1D }, { 2496, 0x1E }, { 2560, 0x1F }
        };

        private static readonly Dictionary<uint, uint> BlackLen10MakeupCodes = new Dictionary<uint, uint>()
        {
            { 64, 0xF }
        };

        private static readonly Dictionary<uint, uint> BlackLen11MakeupCodes = new Dictionary<uint, uint>()
        {
            { 1792, 0x8 }, { 1856, 0xC }, { 1920, 0xD }
        };

        private static readonly Dictionary<uint, uint> BlackLen12MakeupCodes = new Dictionary<uint, uint>()
        {
            { 128, 0xC8 }, { 192, 0xC9 }, { 256, 0x5B }, { 320, 0x33 }, { 384, 0x34 }, { 448, 0x35 },
            { 1984, 0x12 }, { 2048, 0x13 }, { 2112, 0x14 }, { 2176, 0x15 }, { 2240, 0x16 }, { 2304, 0x17 }, { 2368, 0x1C },
            { 2432, 0x1D }, { 2496, 0x1E }, { 2560, 0x1F }
        };

        private static readonly Dictionary<uint, uint> BlackLen13MakeupCodes = new Dictionary<uint, uint>()
        {
            { 512, 0x6C }, { 576, 0x6D }, { 640, 0x4A }, { 704, 0x4B }, { 768, 0x4C }, { 832, 0x4D }, { 896, 0x72 },
            { 960, 0x73 }, { 1024, 0x74 }, { 1088, 0x75 }, { 1152, 0x76 }, { 1216, 0x77 }, { 1280,  0x52 }, { 1344, 0x53 },
            { 1408, 0x54 }, { 1472, 0x55 }, { 1536, 0x5A }, { 1600, 0x5B }, { 1664, 0x64 }, { 1728, 0x65 }
        };

        private int bytePosition;

        private byte bitPosition;

        private IMemoryOwner<byte> compressedDataBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TiffCcittCompressor" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="allocator">The allocator.</param>
        /// <param name="width">The width.</param>
        /// <param name="bitsPerPixel">The bits per pixel.</param>
        protected TiffCcittCompressor(Stream output, MemoryAllocator allocator, int width, int bitsPerPixel)
            : base(output, allocator, width, bitsPerPixel)
        {
            DebugGuard.IsTrue(bitsPerPixel == 1, nameof(bitsPerPixel), "CCITT compression requires one bit per pixel");
            this.bytePosition = 0;
            this.bitPosition = 0;
        }

        private uint GetWhiteMakeupCode(uint runLength, out uint codeLength)
        {
            codeLength = 0;

            if (WhiteLen5MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 5;
                return WhiteLen5MakeupCodes[runLength];
            }

            if (WhiteLen6MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 6;
                return WhiteLen6MakeupCodes[runLength];
            }

            if (WhiteLen7MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 7;
                return WhiteLen7MakeupCodes[runLength];
            }

            if (WhiteLen8MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 8;
                return WhiteLen8MakeupCodes[runLength];
            }

            if (WhiteLen9MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 9;
                return WhiteLen9MakeupCodes[runLength];
            }

            if (WhiteLen11MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 11;
                return WhiteLen11MakeupCodes[runLength];
            }

            if (WhiteLen12MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 12;
                return WhiteLen12MakeupCodes[runLength];
            }

            return 0;
        }

        private uint GetBlackMakeupCode(uint runLength, out uint codeLength)
        {
            codeLength = 0;

            if (BlackLen10MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 10;
                return BlackLen10MakeupCodes[runLength];
            }

            if (BlackLen11MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 11;
                return BlackLen11MakeupCodes[runLength];
            }

            if (BlackLen12MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 12;
                return BlackLen12MakeupCodes[runLength];
            }

            if (BlackLen13MakeupCodes.ContainsKey(runLength))
            {
                codeLength = 13;
                return BlackLen13MakeupCodes[runLength];
            }

            return 0;
        }

        private uint GetWhiteTermCode(uint runLength, out uint codeLength)
        {
            codeLength = 0;

            if (WhiteLen4TermCodes.ContainsKey(runLength))
            {
                codeLength = 4;
                return WhiteLen4TermCodes[runLength];
            }

            if (WhiteLen5TermCodes.ContainsKey(runLength))
            {
                codeLength = 5;
                return WhiteLen5TermCodes[runLength];
            }

            if (WhiteLen6TermCodes.ContainsKey(runLength))
            {
                codeLength = 6;
                return WhiteLen6TermCodes[runLength];
            }

            if (WhiteLen7TermCodes.ContainsKey(runLength))
            {
                codeLength = 7;
                return WhiteLen7TermCodes[runLength];
            }

            if (WhiteLen8TermCodes.ContainsKey(runLength))
            {
                codeLength = 8;
                return WhiteLen8TermCodes[runLength];
            }

            return 0;
        }

        private uint GetBlackTermCode(uint runLength, out uint codeLength)
        {
            codeLength = 0;

            if (BlackLen2TermCodes.ContainsKey(runLength))
            {
                codeLength = 2;
                return BlackLen2TermCodes[runLength];
            }

            if (BlackLen3TermCodes.ContainsKey(runLength))
            {
                codeLength = 3;
                return BlackLen3TermCodes[runLength];
            }

            if (BlackLen4TermCodes.ContainsKey(runLength))
            {
                codeLength = 4;
                return BlackLen4TermCodes[runLength];
            }

            if (BlackLen5TermCodes.ContainsKey(runLength))
            {
                codeLength = 5;
                return BlackLen5TermCodes[runLength];
            }

            if (BlackLen6TermCodes.ContainsKey(runLength))
            {
                codeLength = 6;
                return BlackLen6TermCodes[runLength];
            }

            if (BlackLen7TermCodes.ContainsKey(runLength))
            {
                codeLength = 7;
                return BlackLen7TermCodes[runLength];
            }

            if (BlackLen8TermCodes.ContainsKey(runLength))
            {
                codeLength = 8;
                return BlackLen8TermCodes[runLength];
            }

            if (BlackLen9TermCodes.ContainsKey(runLength))
            {
                codeLength = 9;
                return BlackLen9TermCodes[runLength];
            }

            if (BlackLen10TermCodes.ContainsKey(runLength))
            {
                codeLength = 10;
                return BlackLen10TermCodes[runLength];
            }

            if (BlackLen11TermCodes.ContainsKey(runLength))
            {
                codeLength = 11;
                return BlackLen11TermCodes[runLength];
            }

            if (BlackLen12TermCodes.ContainsKey(runLength))
            {
                codeLength = 12;
                return BlackLen12TermCodes[runLength];
            }

            return 0;
        }

        /// <summary>
        /// Gets the best makeup run length for a given run length
        /// </summary>
        /// <param name="runLength">A run length needing a makeup code</param>
        /// <returns>The makeup length for <paramref name="runLength"/>.</returns>
        protected uint GetBestFittingMakeupRunLength(uint runLength)
        {
            DebugGuard.MustBeGreaterThanOrEqualTo(runLength, MakeupRunLength[0], nameof(runLength));

            for (int i = 0; i < MakeupRunLength.Length - 1; i++)
            {
                if (MakeupRunLength[i] <= runLength && MakeupRunLength[i + 1] > runLength)
                {
                    return MakeupRunLength[i];
                }
            }

            return MakeupRunLength[MakeupRunLength.Length - 1];
        }

        /// <summary>
        /// Gets the terminating code for a run length.
        /// </summary>
        /// <param name="runLength">The run length to get the terminating code for.</param>
        /// <param name="codeLength">The length of the terminating code.</param>
        /// <param name="isWhiteRun">If <c>true</c>, the run is of white pixels.
        /// If <c>false</c> the run is of black pixels</param>
        /// <returns>The terminating code for a run of length <paramref name="runLength"/></returns>
        protected uint GetTermCode(uint runLength, out uint codeLength, bool isWhiteRun)
        {
            if (isWhiteRun)
            {
                return this.GetWhiteTermCode(runLength, out codeLength);
            }

            return this.GetBlackTermCode(runLength, out codeLength);
        }

        /// <summary>
        /// Gets the makeup code for a run length.
        /// </summary>
        /// <param name="runLength">The run length to get the makeup code for.</param>
        /// <param name="codeLength">The length of the makeup code.</param>
        /// <param name="isWhiteRun">If <c>true</c>, the run is of white pixels.
        /// If <c>false</c> the run is of black pixels</param>
        /// <returns>The makeup code for a run of length <paramref name="runLength"/></returns>
        protected uint GetMakeupCode(uint runLength, out uint codeLength, bool isWhiteRun)
        {
            if (isWhiteRun)
            {
                return this.GetWhiteMakeupCode(runLength, out codeLength);
            }

            return this.GetBlackMakeupCode(runLength, out codeLength);
        }

        /// <summary>
        /// Pads output to the next byte
        /// </summary>
        /// <remarks>
        /// If the output is not currently on a byte boundary,
        /// zero-pad it to the next byte
        /// </remarks>
        protected void PadByte()
        {
            // Check if padding is necessary.
            if (this.bitPosition % 8 != 0)
            {
                // Skip padding bits, move to next byte.
                this.bytePosition++;
                this.bitPosition = 0;
            }
        }

        /// <summary>
        /// Writes a code to the output.
        /// </summary>
        /// <param name="codeLength">The length of the code to write.</param>
        /// <param name="code">The code to be written.</param>
        /// <param name="compressedData">The destination buffer to write the code to.</param>
        protected void WriteCode(uint codeLength, uint code, Span<byte> compressedData)
        {
            while (codeLength > 0)
            {
                int bitNumber = (int)codeLength;
                bool bit = (code & (1 << (bitNumber - 1))) != 0;
                if (bit)
                {
                    BitWriterUtils.WriteBit(compressedData, this.bytePosition, this.bitPosition);
                }
                else
                {
                    BitWriterUtils.WriteZeroBit(compressedData, this.bytePosition, this.bitPosition);
                }

                this.bitPosition++;
                if (this.bitPosition == 8)
                {
                    this.bytePosition++;
                    this.bitPosition = 0;
                }

                codeLength--;
            }
        }

        /// <summary>
        /// Writes a image compressed with CCITT T6 to the stream.
        /// </summary>
        /// <param name="pixelsAsGray">The pixels as 8-bit gray array.</param>
        /// <param name="height">The strip height.</param>
        public override void CompressStrip(Span<byte> pixelsAsGray, int height)
        {
            DebugGuard.IsTrue(pixelsAsGray.Length / height == this.Width, "Values must be equals");
            DebugGuard.IsTrue(pixelsAsGray.Length % height == 0, "Values must be equals");

            this.compressedDataBuffer.Clear();
            Span<byte> compressedData = this.compressedDataBuffer.GetSpan();

            this.bytePosition = 0;
            this.bitPosition = 0;

            this.CompressStrip(pixelsAsGray, height, compressedData);

            // Write the compressed data to the stream.
            int bytesToWrite = this.bitPosition != 0 ? this.bytePosition + 1 : this.bytePosition;
            this.Output.Write(compressedData.Slice(0, bytesToWrite));
        }

        /// <summary>
        /// Compress a data strip
        /// </summary>
        /// <param name="pixelsAsGray">The pixels as 8-bit gray array.</param>
        /// <param name="height">The strip height.</param>
        /// <param name="compressedData">The destination for the compressed data.</param>
        protected abstract void CompressStrip(Span<byte> pixelsAsGray, int height, Span<byte> compressedData);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) => this.compressedDataBuffer?.Dispose();

        /// <inheritdoc/>
        public override void Initialize(int rowsPerStrip)
        {
            // This is too much memory allocated, but just 1 bit per pixel will not do, if the compression rate is not good.
            int maxNeededBytes = this.Width * rowsPerStrip;
            this.compressedDataBuffer = this.Allocator.Allocate<byte>(maxNeededBytes);
        }
    }
}
