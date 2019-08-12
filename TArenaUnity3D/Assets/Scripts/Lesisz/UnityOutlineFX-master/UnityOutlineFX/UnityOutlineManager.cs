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
            // Renderers = new List<Renderer>();
         
        }


        public void RemoveOutline()
        {
            foreach (var obj in outliners)
            {
                outlinePostEffect.RemoveRenderers(new List<Renderer>() { obj.renderer });
            }
        }

        public void AddRenderer()
        {
            foreach (var obj in outliners)
            {
                outlinePostEffect.AddRenderers(new List<Renderer>() { obj.renderer });
            }
            foreach (var obj in Renderers)
            {
                outlinePostEffect.AddRenderers(new List<Renderer>() { obj });
            }
        }

        public void ResetOutline()
        {
           // outlinePostEffect.ClearOutlineData();
          

        }


        public void ChangeObj(Renderer obje)
        {
            outliners[0].renderer = obje;
         
        }

        public void AddRenderer(Renderer obje)
        {
            Renderers.Add(obje);
            ResetOutline();
            outlinePostEffect.AddRenderers(new List<Renderer>() { Renderers[0] });
        }
    }

}