using UnityEngine;

public static class RuntimeArtFactory
{
    private static Texture2D circleCache;
    public static Sprite GetCircleSprite(int size, Color color)
    {
        if(circleCache==null || circleCache.width!=size){ circleCache = GenerateCircleTexture(size); }
        // Duplicate texture to tint without altering cache
        var copy = new Texture2D(size,size,TextureFormat.RGBA32,false); copy.SetPixels(circleCache.GetPixels());
        var px = copy.GetPixels();
        for(int i=0;i<px.Length;i++){ if(px[i].a>0.01f){ px[i] = color; } }
        copy.SetPixels(px); copy.Apply();
        return Sprite.Create(copy, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), 64f);
    }

    static Texture2D GenerateCircleTexture(int size)
    {
        var tex = new Texture2D(size,size,TextureFormat.RGBA32,false);
        float r = size/2f; float r2 = r*r; Vector2 c = new Vector2(r,r);
        for(int y=0;y<size;y++){
            for(int x=0;x<size;x++){
                float dx = x-c.x; float dy = y-c.y; float d2 = dx*dx+dy*dy;
                if(d2 <= r2){ tex.SetPixel(x,y, Color.white); } else { tex.SetPixel(x,y, Color.clear); }
            }
        }
        tex.Apply();
        return tex;
    }
}
