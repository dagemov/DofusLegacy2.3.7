using Sunshine.Protocol.IO;
using System;

namespace Sunshine.Protocol.Messages
{
    public class MessagePart
    {
        private readonly bool m_readData;
        private long m_availableBytes;
        private bool m_dataMissing;
        private byte[] m_data;

        public bool IsValid
        {
            get
            {
                bool arg_69_0;
                if (Header.HasValue && Length.HasValue && (!ReadData || Data != null))
                {
                    int? length = Length;
                    long num = ReadData ? Data.Length : m_availableBytes;
                    arg_69_0 = (length.GetValueOrDefault() == num && length.HasValue);
                }
                else
                {
                    arg_69_0 = false;
                }
                return arg_69_0;
            }
        }

        public int? Header
        {
            get;
            private set;
        }

        public int? Id
        {
            get
            {
                int? result;
                if (!Header.HasValue)
                {
                    result = null;
                }
                else
                {
                    result = Header >> 2;
                }
                return result;
            }
        }

        public int? LengthBytesCount
        {
            get
            {
                int? result;
                if (!Header.HasValue)
                {
                    result = null;
                }
                else
                {
                    result = (Header & 3);
                }
                return result;
            }
        }

        public int? Length
        {
            get;
            private set;
        }

        public byte[] Data
        {
            get
            {
                return m_data;
            }
            private set
            {
                m_data = value;
            }
        }

        public bool ReadData
        {
            get
            {
                return m_readData;
            }
        }

        public MessagePart(bool readData)
        {
            m_readData = readData;
        }

        public bool Build(BigEndianReader reader)
        {
            bool result;
            if (reader.BytesAvailable <= 0L)
            {
                result = false;
            }
            else
            {
                if (IsValid)
                {
                    result = true;
                }
                else
                {
                    if (!Header.HasValue && reader.BytesAvailable < 2L)
                    {
                        result = false;
                    }
                    else
                    {
                        if (reader.BytesAvailable >= 2L && !Header.HasValue)
                        {
                            Header = reader.ReadShort();
                        }
                        bool arg_CC_0;
                        if (LengthBytesCount.HasValue)
                        {
                            long num = reader.BytesAvailable;
                            int? num2 = LengthBytesCount;
                            if (num >= num2.GetValueOrDefault() && num2.HasValue)
                            {
                                arg_CC_0 = Length.HasValue;
                                goto IL_CC;
                            }
                        }
                        arg_CC_0 = true;
                        IL_CC:
                        if (!arg_CC_0)
                        {
                            if (LengthBytesCount < 0 || LengthBytesCount > 3)
                            {
                                throw new Exception("Malformated Message Header, invalid bytes number to read message length (inferior to 0 or superior to 3)");
                            }
                            Length = 0;
                            for (int i = LengthBytesCount.Value - 1; i >= 0; i--)
                            {
                                Length |= reader.ReadByte() << i * 8;
                            }
                        }
                        if (Length.HasValue && !m_dataMissing)
                        {
                            if (Length == 0)
                            {
                                if (ReadData)
                                {
                                    Data = new byte[0];
                                }
                                result = true;
                                return result;
                            }
                            long num = reader.BytesAvailable;
                            int? num2 = Length;
                            if (num >= num2.GetValueOrDefault() && num2.HasValue)
                            {
                                if (ReadData)
                                {
                                    Data = reader.ReadBytes(Length.Value);
                                }
                                else
                                {
                                    m_availableBytes = reader.BytesAvailable;
                                }
                                result = true;
                                return result;
                            }
                            num2 = Length;
                            num = reader.BytesAvailable;
                            if (num2.GetValueOrDefault() > num && num2.HasValue)
                            {
                                if (ReadData)
                                {
                                    Data = reader.ReadBytes(reader.BytesAvailable);
                                }
                                else
                                {
                                    m_availableBytes = reader.BytesAvailable;
                                }
                                m_dataMissing = true;
                                result = false;
                                return result;
                            }
                        }
                        else
                        {
                            if (Length.HasValue && m_dataMissing)
                            {
                                long num = (long)(ReadData ? Data.Length : 0) + reader.BytesAvailable;
                                int? num2 = Length;
                                if (num < num2.GetValueOrDefault() && num2.HasValue)
                                {
                                    if (ReadData)
                                    {
                                        int destinationIndex = m_data.Length;
                                        Array.Resize(ref m_data, (int)((long)Data.Length + reader.BytesAvailable));
                                        byte[] array = reader.ReadBytes(reader.BytesAvailable);
                                        Array.Copy(array, 0, Data, destinationIndex, array.Length);
                                    }
                                    else
                                    {
                                        m_availableBytes = reader.BytesAvailable;
                                    }
                                    m_dataMissing = true;
                                }
                                num = (long)(ReadData ? Data.Length : 0) + reader.BytesAvailable;
                                num2 = Length;
                                if (num >= num2.GetValueOrDefault() && num2.HasValue)
                                {
                                    if (ReadData)
                                    {
                                        int num3 = Length.Value - Data.Length;
                                        Array.Resize(ref m_data, Data.Length + num3);
                                        byte[] array = reader.ReadBytes(num3);
                                        Array.Copy(array, 0, Data, Data.Length - num3, num3);
                                    }
                                    else
                                    {
                                        m_availableBytes = reader.BytesAvailable;
                                    }
                                }
                            }
                        }
                        result = IsValid;
                    }
                }
            }
            return result;
        }
    }
}
