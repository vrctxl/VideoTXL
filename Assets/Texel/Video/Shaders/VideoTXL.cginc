#ifndef VIDEOTXL_INCLUDED
#define VIDEOTXL_INCLUDED

uniform float _AspectRatio;
uniform int _FitMode;

float2 TXL_ComputeScreenCorrection(float2 res) {
	float2 normRes = float2(res.x / _AspectRatio, res.y);
	float2 correction;

	if (_FitMode == 2 || (_FitMode == 0 && normRes.x > normRes.y))
		correction = float2(1, normRes.y / normRes.x);
	else if (_FitMode == 1 || (_FitMode == 0 && normRes.x < normRes.y))
		correction = float2(normRes.x / normRes.y, 1);

	return correction;
}

float2 TXL_ApplyScreenCorrection(float2 uv, float2 correction) {
	return ((uv - 0.5) / correction) + 0.5;
}

float TXL_ComputeScreenVisibility(float2 uv, float2 res) {
	float2 uvPadding = (1 / res) * 0.1;
	float2 uvFwidth = fwidth(uv.xy);
	float2 maxf = smoothstep(uvFwidth + uvPadding + 1, uvPadding + 1, uv.xy);
	float2 minf = smoothstep(-uvFwidth - uvPadding, -uvPadding, uv.xy);

	return maxf.x * maxf.y * minf.x * minf.y;
}

bool TXL_ShouldApplyScreenCorrection(float2 res) {
	float curAspectRatio = res.x / res.y;
	return abs(curAspectRatio - _AspectRatio) > .001 && _FitMode != 3;
}

void TXL_ComputeScreenFit(float2 uv, float2 res, out float2 uvFit, out float visibility) {
	visibility = 1;

	if (TXL_ShouldApplyScreenCorrection(res)) {
		float2 correction = TXL_ComputeScreenCorrection(res);
		uv = TXL_ApplyScreenCorrection(uv, correction);
		visibility = TXL_ComputeScreenVisibility(uv, res);
	}

	uvFit = uv;
}

#endif
