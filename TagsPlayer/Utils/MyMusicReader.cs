using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace TagsPlayer.Model
{

    public class MyMusicReader : WaveStream
    {
        public class MediaFoundationReaderSettings
        {
            public bool RequestFloatOutput
            {
                get;
                set;
            }

            public bool SingleReaderObject
            {
                get;
                set;
            }

            public bool RepositionInRead
            {
                get;
                set;
            }

            public MediaFoundationReaderSettings()
            {
                RepositionInRead = true;
            }
        }

        private WaveFormat waveFormat;

        private long length;

        private MediaFoundationReaderSettings settings;

        private readonly string file;

        private readonly IStream stream;

        private IMFSourceReader pReader;

        private long position;

        private byte[] decoderOutputBuffer;

        private int decoderOutputOffset;

        private int decoderOutputCount;

        private long repositionTo = -1L;

        public override WaveFormat WaveFormat => waveFormat;

        public override long Length => length;

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Position cannot be less than 0");
                }

                if (settings.RepositionInRead)
                {
                    repositionTo = value;
                    position = value;
                }
                else
                {
                    Reposition(value);
                }
            }
        }

        public event EventHandler WaveFormatChanged;

        protected MyMusicReader()
        {
        }

        public MyMusicReader(string file)
            : this(file, null)
        {
        }

        public MyMusicReader(string file, MediaFoundationReaderSettings settings)
        {
            this.file = file;
            Init(settings);
        }

        public MyMusicReader(MemoryStream stream)
        {
            this.stream = new MemoryStreamIStreamAdapter(stream);
            InitFromStream(settings);
        }

        protected void InitFromStream(MediaFoundationReaderSettings initialSettings)
        {
            MediaFoundationApi.Startup();
            settings = (initialSettings ?? new MediaFoundationReaderSettings());
            IMFSourceReader iMFSourceReader = CreateReaderFromStream(settings);
            waveFormat = GetCurrentWaveFormat(iMFSourceReader);
            iMFSourceReader.SetStreamSelection(-3, pSelected: true);
            length = GetLength(iMFSourceReader);
            if (settings.SingleReaderObject)
            {
                pReader = iMFSourceReader;
            }
            else
            {
                Marshal.ReleaseComObject(iMFSourceReader);
            }
        }

        protected void Init(MediaFoundationReaderSettings initialSettings)
        {
            MediaFoundationApi.Startup();
            settings = (initialSettings ?? new MediaFoundationReaderSettings());
            IMFSourceReader iMFSourceReader = CreateReader(settings);
            waveFormat = GetCurrentWaveFormat(iMFSourceReader);
            iMFSourceReader.SetStreamSelection(-3, pSelected: true);
            length = GetLength(iMFSourceReader);
            if (settings.SingleReaderObject)
            {
                pReader = iMFSourceReader;
            }
            else
            {
                Marshal.ReleaseComObject(iMFSourceReader);
            }
        }

        private WaveFormat GetCurrentWaveFormat(IMFSourceReader reader)
        {
            reader.GetCurrentMediaType(-3, out IMFMediaType ppMediaType);
            MediaType mediaType = new MediaType(ppMediaType);
            _ = mediaType.MajorType;
            Guid subType = mediaType.SubType;
            int channelCount = mediaType.ChannelCount;
            int bitsPerSample = mediaType.BitsPerSample;
            int sampleRate = mediaType.SampleRate;
            if (subType == AudioSubtypes.MFAudioFormat_PCM)
            {
                return new WaveFormat(sampleRate, bitsPerSample, channelCount);
            }

            if (subType == AudioSubtypes.MFAudioFormat_Float)
            {
                return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
            }

            string str = FieldDescriptionHelper.Describe(typeof(AudioSubtypes), subType);
            throw new InvalidDataException("Unsupported audio sub Type " + str);
        }

        private static MediaType GetCurrentMediaType(IMFSourceReader reader)
        {
            reader.GetCurrentMediaType(-3, out IMFMediaType ppMediaType);
            return new MediaType(ppMediaType);
        }

        protected virtual IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromURL(file, null, out IMFSourceReader ppSourceReader);
            ppSourceReader.SetStreamSelection(-2, pSelected: false);
            ppSourceReader.SetStreamSelection(-3, pSelected: true);
            MediaType mediaType = new MediaType();
            mediaType.MajorType = MediaTypes.MFMediaType_Audio;
            mediaType.SubType = (settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM);
            MediaType currentMediaType = GetCurrentMediaType(ppSourceReader);
            mediaType.ChannelCount = currentMediaType.ChannelCount;
            mediaType.SampleRate = currentMediaType.SampleRate;
            try
            {
                ppSourceReader.SetCurrentMediaType(-3, IntPtr.Zero, mediaType.MediaFoundationObject);
            }
            catch (COMException exception) when (exception.GetHResult() == -1072875852)
            {
                if (!(currentMediaType.SubType == AudioSubtypes.MFAudioFormat_AAC) || currentMediaType.ChannelCount != 1)
                {
                    throw;
                }

                mediaType.SampleRate = (currentMediaType.SampleRate *= 2);
                mediaType.ChannelCount = (currentMediaType.ChannelCount *= 2);
                ppSourceReader.SetCurrentMediaType(-3, IntPtr.Zero, mediaType.MediaFoundationObject);
            }

            Marshal.ReleaseComObject(currentMediaType.MediaFoundationObject);
            return ppSourceReader;
        }

        protected virtual IMFSourceReader CreateReaderFromStream(MediaFoundationReaderSettings settings)
        {
            IMFByteStream mfByteStream;
            MediaFoundationInterop.MFCreateMFByteStreamOnStreamEx(stream, out mfByteStream);
            MediaFoundationInterop.MFCreateSourceReaderFromByteStream(mfByteStream, null, out IMFSourceReader ppSourceReader);
            ppSourceReader.SetStreamSelection(-2, pSelected: false);
            ppSourceReader.SetStreamSelection(-3, pSelected: true);
            MediaType mediaType = new MediaType();
            mediaType.MajorType = MediaTypes.MFMediaType_Audio;
            mediaType.SubType = (settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM);
            MediaType currentMediaType = GetCurrentMediaType(ppSourceReader);
            mediaType.ChannelCount = currentMediaType.ChannelCount;
            mediaType.SampleRate = currentMediaType.SampleRate;
            try
            {
                ppSourceReader.SetCurrentMediaType(-3, IntPtr.Zero, mediaType.MediaFoundationObject);
            }
            catch (COMException exception) when (exception.GetHResult() == -1072875852)
            {
                if (!(currentMediaType.SubType == AudioSubtypes.MFAudioFormat_AAC) || currentMediaType.ChannelCount != 1)
                {
                    throw;
                }

                mediaType.SampleRate = (currentMediaType.SampleRate *= 2);
                mediaType.ChannelCount = (currentMediaType.ChannelCount *= 2);
                ppSourceReader.SetCurrentMediaType(-3, IntPtr.Zero, mediaType.MediaFoundationObject);
            }

            Marshal.ReleaseComObject(currentMediaType.MediaFoundationObject);
            return ppSourceReader;
        }

        private long GetLength(IMFSourceReader reader)
        {
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
            try
            {
                int presentationAttribute = reader.GetPresentationAttribute(-1, MediaFoundationAttributes.MF_PD_DURATION, intPtr);
                switch (presentationAttribute)
                {
                    case -1072875802:
                        return 0L;
                    default:
                        Marshal.ThrowExceptionForHR(presentationAttribute);
                        break;
                    case 0:
                        break;
                }

                return (long)Marshal.PtrToStructure<PropVariant>(intPtr)!.Value * waveFormat.AverageBytesPerSecond / 10000000;
            }
            finally
            {
                PropVariant.Clear(intPtr);
                Marshal.FreeHGlobal(intPtr);
            }
        }

        private void EnsureBuffer(int bytesRequired)
        {
            if (decoderOutputBuffer == null || decoderOutputBuffer.Length < bytesRequired)
            {
                decoderOutputBuffer = new byte[bytesRequired];
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (pReader == null)
            {
                pReader = CreateReaderFromStream(settings);
            }

            if (repositionTo != -1)
            {
                Reposition(repositionTo);
            }

            int num = 0;
            if (decoderOutputCount > 0)
            {
                num += ReadFromDecoderBuffer(buffer, offset, count - num);
            }

            while (num < count)
            {
                pReader.ReadSample(-3, 0, out int _, out MF_SOURCE_READER_FLAG pdwStreamFlags, out ulong _, out IMFSample ppSample);
                if ((pdwStreamFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                {
                    break;
                }

                if ((pdwStreamFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0)
                {
                    waveFormat = GetCurrentWaveFormat(pReader);
                    OnWaveFormatChanged();
                }
                else if (pdwStreamFlags != 0)
                {
                    throw new InvalidOperationException($"MediaFoundationReadError {pdwStreamFlags}");
                }

                ppSample.ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);
                ppBuffer.Lock(out IntPtr ppbBuffer, out int _, out int pcbCurrentLength);
                EnsureBuffer(pcbCurrentLength);
                Marshal.Copy(ppbBuffer, decoderOutputBuffer, 0, pcbCurrentLength);
                decoderOutputOffset = 0;
                decoderOutputCount = pcbCurrentLength;
                num += ReadFromDecoderBuffer(buffer, offset + num, count - num);
                ppBuffer.Unlock();
                Marshal.ReleaseComObject(ppBuffer);
                Marshal.ReleaseComObject(ppSample);
            }

            position += num;
            return num;
        }

        private int ReadFromDecoderBuffer(byte[] buffer, int offset, int needed)
        {
            int num = Math.Min(needed, decoderOutputCount);
            Array.Copy(decoderOutputBuffer, decoderOutputOffset, buffer, offset, num);
            decoderOutputOffset += num;
            decoderOutputCount -= num;
            if (decoderOutputCount == 0)
            {
                decoderOutputOffset = 0;
            }

            return num;
        }

        private void Reposition(long desiredPosition)
        {
            PropVariant structure = PropVariant.FromLong(10000000 * repositionTo / waveFormat.AverageBytesPerSecond);
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            try
            {
                Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
                pReader.SetCurrentPosition(Guid.Empty, intPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }

            decoderOutputCount = 0;
            decoderOutputOffset = 0;
            position = desiredPosition;
            repositionTo = -1L;
        }

        protected override void Dispose(bool disposing)
        {
            if (pReader != null)
            {
                Marshal.ReleaseComObject(pReader);
                pReader = null;
            }

            base.Dispose(disposing);
        }

        private void OnWaveFormatChanged()
        {
            this.WaveFormatChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class MemoryStreamIStreamAdapter : IStream
    {
        private MemoryStream _memoryStream;

        public MemoryStreamIStreamAdapter(MemoryStream memoryStream)
        {
            _memoryStream = memoryStream;
        }

        public void Clone(out IStream ppstm)
        {
            ppstm = new MemoryStreamIStreamAdapter(new MemoryStream(_memoryStream.ToArray()));
        }

        public void Commit(int grfCommitFlags)
        {
            // MemoryStream没有实现Commit功能，因此此方法为空
        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            byte[] buffer = new byte[cb];
            int bytesRead = _memoryStream.Read(buffer, 0, (int)cb);
            pstm.Write(buffer, bytesRead, pcbWritten);

            if (pcbRead != IntPtr.Zero)
            {
                Marshal.WriteInt64(pcbRead, bytesRead);
            }
        }

        public void LockRegion(long libOffset, long cb, int dwLockType)
        {
            // MemoryStream不支持锁定，所以此方法为空
        }

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            int bytesRead = _memoryStream.Read(pv, 0, cb);

            if (pcbRead != IntPtr.Zero)
            {
                Marshal.WriteInt64(pcbRead, bytesRead);
            }
        }

        public void Revert()
        {
            // MemoryStream没有实现Revert功能，因此此方法为空
        }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            SeekOrigin origin = (SeekOrigin)dwOrigin;
            long newPosition = _memoryStream.Seek(dlibMove, origin);

            if (plibNewPosition != IntPtr.Zero)
            {
                Marshal.WriteInt64(plibNewPosition, newPosition);
            }
        }

        public void SetSize(long libNewSize)
        {
            _memoryStream.SetLength(libNewSize);
        }

        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG
            {
                cbSize = _memoryStream.Length,
                type = 2, // STGTY_STREAM
            };
        }

        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            // MemoryStream不支持解锁，所以此方法为空
        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            _memoryStream.Write(pv, 0, cb);

            if (pcbWritten != IntPtr.Zero)
            {
                Marshal.WriteInt64(pcbWritten, cb);
            }
        }
    }
}
