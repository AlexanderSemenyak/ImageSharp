// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Threading;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Formats.Tiff
{
    /// <summary>
    /// Image decoder for generating an image out of a TIFF stream.
    /// </summary>
    public class TiffDecoder : IImageDecoder, ITiffDecoderOptions, IImageInfoDetector
    {
        /// <summary>
        /// Gets or sets a value indicating whether the metadata should be ignored when the image is being decoded.
        /// </summary>
        public bool IgnoreMetadata { get; set; }

        /// <summary>
        /// Gets or sets the decoding mode for multi-frame images.
        /// </summary>
        public FrameDecodingMode DecodingMode { get; set; }

        /// <inheritdoc/>
        public Image<TPixel> Decode<TPixel>(Configuration configuration, Stream stream, CancellationToken cancellationToken)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            Guard.NotNull(stream, nameof(stream));

            var decoder = new TiffDecoderCore(configuration, this);
            return decoder.Decode<TPixel>(configuration, stream, cancellationToken);
        }

        /// <inheritdoc/>
        public Image Decode(Configuration configuration, Stream stream, CancellationToken cancellationToken)
            => this.Decode<Rgba32>(configuration, stream, cancellationToken);

        /// <inheritdoc/>
        public IImageInfo Identify(Configuration configuration, Stream stream, CancellationToken cancellationToken)
        {
            Guard.NotNull(stream, nameof(stream));

            var decoder = new TiffDecoderCore(configuration, this);
            return decoder.Identify(configuration, stream, cancellationToken);
        }
    }
}
