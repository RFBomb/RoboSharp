using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSharp.Extensions.Helpers;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// <see cref="IFileCopierFactory"/> that produces <see cref="StreamedCopier"/> objects
    /// <br/>This class is platform agnostic.
    /// </summary>
    public class StreamedCopierFactory: AbstractFileCopierFactory<StreamedCopier>, IFileCopierFactory
    {
        /// <inheritdoc cref="StreamedCopier.BufferSize"/>
        public int BufferSize { get; set; } = StreamedCopier.DefaultBufferSize;

        /// <inheritdoc/>
        public override StreamedCopier Create(FileInfo source, FileInfo destination, IDirectoryPair parent)
        {
            return new StreamedCopier(source, destination, parent)
            {
                BufferSize = BufferSize
            };
        }
    }
}
