using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ESPUnturned
{
    public class Entity
    {

        public IntPtr address { get; set; }
        public IntPtr _player { get; set; }
        public IntPtr _life { get; set; }

        public IntPtr _movement { get; set; }
        public IntPtr _cachedPtr { get; set; }

        public IntPtr _playerMovement { get; set; }
        public byte health { get; set; }
        public byte isDead { get; set; }
        public byte stamina { get; set; }
        public string name { get; set; }
        public Vector3 origin { get; set; }
        public Vector3 abs { get; set; }
        public Vector2 originScreenPosition { get; set; }
        public Vector2 absScreenPosition { get; set; }
        public Vector3 viewOffset { get; set; }
        public float magnitude { get; set; }
        public IntPtr _vehicle { get; set; }
    }
}