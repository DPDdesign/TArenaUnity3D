using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xenu.Game {

	public class UnityOutlineManager : MonoBehaviour {
		[System.Serializable]
		public class OutlineData
		{
			public Renderer renderer;
		}

		public UnityOutlineFX outlinePostEffect;
		public OutlineData[] outliners;
        public List<Renderer> Renderers;
		private void Start()
		{
            outlinePostEffect = FindObjectOfType<UnityOutlineFX>();
            Renderers = new List<Renderer>();
         
        }
        public bool isCamera()
        {
            if (outlinePostEffect != null)
            {
                return true;
            }
            else
            {
                outlinePostEffect = FindObjectOfType<UnityOutlineFX>();
                return false;

            }
        }

        public void RemoveOutline()
        {
            if (!isCamera()) return;
              
            foreach (var obj in outliners)
            {
                outlinePostEffect.RemoveRenderers(new List<Renderer>() { obj.renderer });
            }
            foreach (var obj in Renderers)
            {
                outlinePostEffect.RemoveRenderers(new List<Renderer>() { obj });
            }

            outlinePostEffect.ClearOutlineData();
        }

        public void AddRenderer()
        {
            if (!isCamera()) return;
            RemoveOutline();
            outlinePostEffect.ClearOutlineData();
            Debug.LogError("Test");
            foreach (var obj in outliners)
            {
                outlinePostEffect.AddRenderers(new List<Renderer>() { obj.renderer });
            }
            foreach (var obj in Renderers)
            {
                outlinePostEffect.AddRenderers(new List<Renderer>() { obj });
            }
        }
        public void AddMainOutlineWithReset()
        {
            if (!isCamera()) return;

            outlinePostEffect.AddRenderers(new List<Renderer>() { outliners[0].renderer });
        }



        public void ChangeObj(Renderer obje)
        {
            if (!isCamera()) return;
            RemoveAllOnlyMain();
            outliners[0].renderer = obje;       
        }

        public void ChangeObjects(List<Renderer>  objects)
        {
            if (!isCamera()) return;
            int i = 1;
            foreach (Renderer r in objects)
            {
               
                outliners[i].renderer = r;
                outlinePostEffect.AddRenderers(new List<Renderer>() { outliners[i].renderer });
                i++;
            }
           
        }
        public void RemoveObjects(List<Renderer> objects)
        {
            if (!isCamera()) return;
            int i = 1;
            foreach (Renderer r in objects)
            {
                outlinePostEffect.RemoveRenderers(new List<Renderer>() { outliners[i].renderer });
                outliners[i].renderer = null;
                i++;
            }

        }

        public void RemoveAllOnlyMain()
        {
            if (!isCamera()) return;

            outlinePostEffect.RemoveRenderers(new List<Renderer>() { outliners[0].renderer });
            outliners[0].renderer = null;


            outlinePostEffect.RecreateCommandBuff();
        }

        public void RemoveAllButMain()
        {
            if (!isCamera()) return;
            int i = 0;
            foreach (var obj in outliners)
            {
                if (i > 0)
                {
                    outlinePostEffect.RemoveRenderers(new List<Renderer>() { outliners[i].renderer });
                    outliners[i].renderer = null;
                }
                i++;
            }
            outlinePostEffect.RecreateCommandBuff();
        }

    }

}