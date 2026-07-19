#ifndef VIDEOTXL_UNLIT360_INCLUDED
#define VIDEOTXL_UNLIT360_INCLUDED

#include "UnityCG.cginc"

static const float UNLIT360_INV_PI = 0.31830988618;
static const float UNLIT360_INV_2PI = 0.15915494309;

// From nvidia reference implementation
float Unlit360_ApproxAsin(float x)
{
    float negate = float(x < 0);
    x = abs(x);

    float ret = -0.0187293;
    ret = ret * x + 0.0742610;
    ret = ret * x - 0.2121144;
    ret = ret * x + 1.5707288;
    ret = 1.57079632679 - sqrt(1.0 - x) * ret;

    return ret - 2.0 * negate * ret;
}

// From nvidia reference implementation
float Unlit360_ApproxAtan2(float y, float x)
{
    float ax = abs(x);
    float ay = abs(y);

    float t0 = max(ax, ay);
    float t1 = min(ax, ay);

    // Avoid divide-by-zero if dir is degenerate
    float t3 = (t0 > 1e-8) ? (t1 / t0) : 0.0;

    float t4 = t3 * t3;

    float t = -0.013480470;
    t = t * t4 + 0.057477314;
    t = t * t4 - 0.121239071;
    t = t * t4 + 0.195635925;
    t = t * t4 - 0.332994597;
    t = t * t4 + 0.999995630;
    t3 = t * t3;

    if (ay > ax)
        t3 = 1.570796327 - t3;
    if (x < 0.0)
        t3 = 3.141592654 - t3;
    if (y < 0.0)
        t3 = -t3;

    return t3;
}

// Convert a direction vector to equirectangular UV
float2 Unlit360_EquirectUV(float3 dir)
{
    dir = normalize(dir);

    float2 uv;
    uv.x = Unlit360_ApproxAtan2(dir.x, dir.z) * UNLIT360_INV_2PI + 0.5;
    uv.y = Unlit360_ApproxAsin(dir.y) * UNLIT360_INV_PI + 0.5;

    return uv;
}

// Apply yaw rotation, flips, and wrap
float2 Unlit360_AdjustUV(float2 uv, float yawOffset, float flipU, float flipV)
{
    uv.x += yawOffset;

    if (flipU > 0.5)
        uv.x = 1.0 - uv.x;
    if (flipV > 0.5)
        uv.y = 1.0 - uv.y;

    uv.x = frac(uv.x);

    return uv;
}

// Seam-safe gradient fix for wrapped equirect U
void Unlit360_FixWrappedGradients(float2 uv, inout float2 dx, inout float2 dy)
{
    dx.x = frac(uv.x + dx.x) - uv.x;
    dy.x = frac(uv.x + dy.x) - uv.x;
}

// Sample equirect texture with seam-safe wrapped U derivatives
fixed4 Unlit360_SampleEquirectGrad(sampler2D tex, float2 uv)
{
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);

    Unlit360_FixWrappedGradients(uv, dx, dy);

    return tex2Dgrad(tex, uv, dx, dy);
}

#endif