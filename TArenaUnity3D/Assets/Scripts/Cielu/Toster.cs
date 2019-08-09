using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tostery 
{

    public class Toster : MonoBehaviour
    {
        public string Name = "Zwkly Toster";
        public int BaseHealthPoints = 1;
        public int BaseDamage = 1;
        public int BaseDefence = 1;
        public int hp = 1;
        public int id;

        public void WriteStats()
        {
            Debug.Log("Toster " + Name + " Stats (Hp/Dmg/Def): " + hp + " / " + BaseDamage + " / " + BaseDefence);

        }

        public virtual void Start()
        {
            SetValues(id);
        }

        void OnMouseDown()
        {

            Hello();
        

        }

        public virtual void Hello()
        {
            Debug.Log("Jestem Toster "+ this.Name);
        }

        public virtual void DealHp(int x)
        {
            hp -= x;
            Debug.Log(hp);
        }

        public virtual void AddHp(int x)
        {
            hp += x;
        }

        public virtual void SetValues(int id)
        {
            this.Name = __Tostery_Staty.nazwy[id];
            this.hp = __Tostery_Staty.statystyki[id,0];
            this.BaseHealthPoints = __Tostery_Staty.statystyki[id, 0];
            this.BaseDamage = __Tostery_Staty.statystyki[id, 1];
            this.BaseDefence = __Tostery_Staty.statystyki[id, 2];
        }


        public void GetValues (int id)
        {
        }
    }



    



}

