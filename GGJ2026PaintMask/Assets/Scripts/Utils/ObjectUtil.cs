namespace GGJ2026.Utils
{
    public static class ObjectUtil
    {
        public static void ReleaseObject<T>(ref T obj) 
			where T : UnityEngine.Object
		{
			if (!obj)
			{
				obj = null;
				return;
			}
			UnityEngine.Object.Destroy(obj);
			obj = null;
		}
    }
}