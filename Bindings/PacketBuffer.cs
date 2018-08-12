using System;
using System.Collections.Generic;
using System.Text;

namespace Bindings {
    class PacketBuffer : IDisposable {
        public int readPosition { get { return _readPosition; } }
        public int count { get { return _bufferList.Count; } }
        public int length { get { return count - readPosition; } }

        private List<byte> _bufferList;
        private byte[] _readBuffer;
        private int _readPosition;
        private bool _buffUpdate = false;

        public PacketBuffer() {
            _bufferList = new List<byte>();
            _readPosition = 0;
        }

        public PacketBuffer(byte[] data) {
            _bufferList = new List<byte>();
            _readPosition = 0;

            WriteBytes(data);
        }

        public byte[] ToArray() {
            return _bufferList.ToArray();
        }

        public void Clear() {
            _bufferList.Clear();
            _readPosition = 0;
        }

        // Write Data
        public void WriteServerPacket(ServerPackets serverPacket) {
            WriteInteger((int)serverPacket);
        }

        public void WriteClientPacket(ClientPackets clientPacket) {
            WriteInteger((int)clientPacket);
        }

        public void WriteBytes(byte[] input) {
            _bufferList.AddRange(input);
            _buffUpdate = true;
        }

        public void WriteByte(byte input) {
            _bufferList.Add(input);
            _buffUpdate = true;
        }

        public void WriteInteger(int input) {
            _bufferList.AddRange(BitConverter.GetBytes(input));
            _buffUpdate = true;
        }

        public void WriteFloat(float input) {
            _bufferList.AddRange(BitConverter.GetBytes(input));
            _buffUpdate = true;
        }

        public void WriteString(string input) {
            _bufferList.AddRange(BitConverter.GetBytes(input.Length));
            _bufferList.AddRange(Encoding.ASCII.GetBytes(input));
            _buffUpdate = true;
        }

        // Read Data
        public ServerPackets ReadServerPacket() {
            return (ServerPackets)ReadInteger();
        }

        public ClientPackets ReadClientPacket() {
            return (ClientPackets)ReadInteger();
        }

        public int ReadInteger(bool peek = true) {
            if (_bufferList.Count > _readPosition) {
                if (_buffUpdate) {
                    _readBuffer = _bufferList.ToArray();
                    _buffUpdate = false;
                }

                int value = BitConverter.ToInt32(_readBuffer, _readPosition);

                if (peek & _bufferList.Count > _readPosition) {
                    _readPosition += 4;
                }

                return value;
            } else {
                throw new Exception("Buffer is past its Limit!");
            }
        }

        public float ReadFloat(bool peek = true) {
            if (_bufferList.Count > _readPosition) {
                if (_buffUpdate) {
                    _readBuffer = _bufferList.ToArray();
                    _buffUpdate = false;
                }

                float value = BitConverter.ToSingle(_readBuffer, _readPosition);

                if (peek & _bufferList.Count > _readPosition) {
                    _readPosition += 4;
                }

                return value;
            } else {
                throw new Exception("Buffer is past its Limit!");
            }
        }

        public byte ReadByte(bool peek = true) {
            if (_bufferList.Count > _readPosition) {
                if (_buffUpdate) {
                    _readBuffer = _bufferList.ToArray();
                    _buffUpdate = false;
                }

                byte value = _readBuffer[_readPosition];

                if (peek & _bufferList.Count > _readPosition) {
                    _readPosition++;
                }

                return value;
            } else {
                throw new Exception("Buffer is past its Limit!");
            }
        }

        public byte[] ReadBytes(int length, bool peek = true) {
            if (_buffUpdate) {
                _readBuffer = _bufferList.ToArray();
                _buffUpdate = false;
            }

            byte[] value = _bufferList.GetRange(_readPosition, length).ToArray();

            if (peek & _bufferList.Count > _readPosition) {
                _readPosition += length;
            }

            return value;
        }

        public string ReadString(bool peek = true) {
            int length = ReadInteger(true);

            if (_buffUpdate) {
                _readBuffer = _bufferList.ToArray();
                _buffUpdate = false;
            }

            string value = Encoding.ASCII.GetString(_readBuffer, _readPosition, length);

            if (peek & _bufferList.Count > _readPosition) {
                _readPosition += length;
            }

            return value;
        }

        // IDisposable
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    _bufferList.Clear();
                }

                _readPosition = 0;
            }

            disposedValue = true;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
