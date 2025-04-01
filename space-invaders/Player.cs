using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Space_intruders
{
    public class Player
    {
        private int ID;
        private int HP;
        private int Speed;
        private int Armour;
        private int DMG;

        public int GetID() { return this.ID; }
        public int GetHP() { return this.HP; }
        public int GetSpeed() { return this.Speed; }
        public int GetArmour() { return this.Armour; }
        public int GetDMG() { return this.DMG; }

        public void SetID(int id) { this.ID = id; }
        public void SetHP(int hp) { this.HP = hp; }
        public void SetSpeed(int speed) { this.Speed = speed; }
        public void SetArmour(int armour) { this.Armour = armour; }
        public void SetDMG(int dmg) { this.DMG = dmg; }
    }
}
