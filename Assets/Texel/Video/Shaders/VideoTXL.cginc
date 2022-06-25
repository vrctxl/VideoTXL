#ifndef VIDEOTXL_INCLUDED
#define VIDEOTXL_INCLUDED

uniform float _AspectRatio;
uniform int _FitMode;

void TXL_ComputeScreenFit(float2 uv, float2 res, out float2 uvFit, out float visibility) {
	float curAspectRatio = res.x / res.y;

	visibility = 1;

	if (abs(curAspectRatio - _AspectRatio) > .001 && _FitMode != 3) {
		float2 normRes = float2(res.x / _AspectRatio, res.y);
		float2 correction;

		if (_FitMode == 2 || (_FitMode == 0 && normRes.x > normRes.y))
			correction = float2(1, normRes.y / normRes.x);
		else if (_FitMode == 1 || (_FitMode == 0 && normRes.x < normRes.y))
			correction = float2(normRes.x / normRes.y, 1);

		uv = ((uv - 0.5) / correction) + 0.5;

		float2 uvPadding = (1 / res) * 0.1;
		float2 uvFwidth = fwidth(uv.xy);
		float2 maxf = smoothstep(uvFwidth + uvPadding + 1, uvPadding + 1, uv.xy);
		float2 minf = smoothstep(-uvFwidth - uvPadding, -uvPadding, uv.xy);

		visibility = maxf.x * maxf.y * minf.x * minf.y;
	}

	uvFit = uv;
}

#endif
