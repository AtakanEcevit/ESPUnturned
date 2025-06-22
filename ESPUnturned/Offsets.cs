using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPUnturned
{
    public class Offsets
    {
        public int _items = 0x10;
        public int _steamPlayer0 = 0x20;
        public int _player = 0x30;
        public int _movement = 0x78;
        public int _cachedPtr = 0x10;
        public int _playerMovement = 0x28;
        public int _channel = 0x38;
        public int isOwner = 0x2D; //localPlayer flag'i

        public int _life = 0x60;
        public int isDead = 0xC8;
        public int health = 0xCA;
        public int stamina = 0xD4;

        public int originX = 0x114;
        public int originY = 0x118;
        public int originZ = 0x11C;

        public int _vehicle = 0xD8;
        public int originXinVehicle = 0x2c4;
        public int originYinVehicle = 0x2c8;
        public int originZinVehicle = 0x2cc;


        public int _size = 0x18;
    }
}