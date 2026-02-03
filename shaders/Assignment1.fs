#version 330 core
out vec4 FragColor;

uniform float u_time;
uniform vec2  u_resolution;
uniform vec2  u_mouse;     // 0..1 bottom-left
uniform int   u_skyMode;   // 0 night, 1 day, 2 purple

float hash11(float x) { return fract(sin(x * 127.1) * 43758.5453123); }
float hash21(vec2 p)  { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123); }

vec3 skyColor(float y01, vec2 p, float t, int mode)
{
    if (mode == 1) {
        // DAY
        vec3 topCol = vec3(0.35, 0.65, 0.95);
        vec3 midCol = vec3(0.65, 0.80, 0.98);
        vec3 horCol = vec3(0.96, 0.92, 0.86);
        vec3 sky = mix(horCol, midCol, smoothstep(0.0, 0.55, y01));
        sky = mix(sky, topCol, smoothstep(0.55, 1.0, y01));
        float haze = 0.5 + 0.5 * sin(t * 0.08 + p.x * 0.8);
        sky += vec3(0.06, 0.08, 0.10) * haze * smoothstep(0.0, 0.7, y01);
        return sky;
    }
    if (mode == 0) {
        // NIGHT
        vec3 topCol = vec3(0.03, 0.04, 0.10);
        vec3 midCol = vec3(0.05, 0.06, 0.14);
        vec3 horCol = vec3(0.10, 0.07, 0.14);
        vec3 sky = mix(horCol, midCol, smoothstep(0.0, 0.55, y01));
        sky = mix(sky, topCol, smoothstep(0.55, 1.0, y01));
        float band = 0.5 + 0.5 * sin(t * 0.18 + p.x * 1.1 + p.y * 2.2);
        sky += vec3(0.03, 0.06, 0.12) * band * smoothstep(0.2, 1.0, y01);
        return sky;
    }

    // PURPLE
    vec3 topCol = vec3(0.09, 0.03, 0.18);
    vec3 midCol = vec3(0.35, 0.10, 0.45);
    vec3 horCol = vec3(0.75, 0.20, 0.55);
    vec3 sky = mix(horCol, midCol, smoothstep(0.0, 0.55, y01));
    sky = mix(sky, topCol, smoothstep(0.55, 1.0, y01));
    float band = 0.5 + 0.5 * sin(t * 0.25 + p.x * 1.2 + p.y * 2.4);
    sky += vec3(0.10, 0.04, 0.16) * band * smoothstep(0.2, 1.0, y01);
    return sky;
}

// Returns: layer color (NOT premultiplied) + alpha (occlusion mask)
// Hover floor ripple: only used when enableHoverFloor = true (front layer)
struct LayerOut { vec3 rgb; float a; };

LayerOut buildingsLayer(
    vec2 p, vec2 m, float t,
    float horizon, float scrollSpeed,
    float colW, vec2 bwRange, vec2 hRange,
    float winColsMin, float winColsMax,
    float winRowsMin, float winRowsMax,
    vec3 baseDarkA, vec3 baseDarkB,
    vec3 edgeTint,
    vec3 warmLight,
    bool enableHoverFloor
){

float scroll = t * scrollSpeed;

    float worldX = p.x + scroll;
    float gx = floor(worldX / colW);
    float fx = fract(worldX / colW) - 0.5;

    float r1 = hash11(gx * 1.77);
    float r2 = hash11(gx * 3.31 + 10.0);

    float bw = mix(bwRange.x, bwRange.y, r2);
    float h  = mix(hRange.x,  hRange.y,  r1);
    float yTop = -1.0 + h;

    // Body mask (this is the occlusion alpha)
    float body = step(abs(fx), bw) * step(p.y, yTop);

    // roof
    float roofW = bw * (0.55 + 0.25 * sin(r2 * 9.0));
    float roof = step(abs(fx), roofW) * step(p.y, yTop + 0.02);
    body = max(body, roof * 0.9);

    float alpha = body;

    // building base + edges
    vec3 bBase = mix(baseDarkA, baseDarkB, r2);
    float edge = smoothstep(bw, bw - 0.018, abs(fx)) * step(p.y, yTop);
    vec3 bCol = bBase + edge * edgeTint;

    // window grid (big rooms)
    vec2 local;
    local.x = (fx / (bw + 1e-6)) * 0.5 + 0.5;          // 0..1
    local.y = (p.y - (-1.0)) / (h + 1e-6);             // 0..1

    float winCols = mix(winColsMin, winColsMax, r2);
    float winRows = mix(winRowsMin, winRowsMax, r1);

    vec2 wuv = vec2(local.x * winCols, local.y * winRows);
    vec2 wid = floor(wuv);
    vec2 wfr = fract(wuv);

    // thick frame
    float frame = 0.17;
    float windowShape =
        step(frame, wfr.x) * step(frame, wfr.y) *
        step(wfr.x, 1.0 - frame) * step(wfr.y, 1.0 - frame);

    float isWindow = body * windowShape;

    // stable on/off per room + flicker
    float seed = hash21(wid + vec2(gx * 4.0, gx * 9.0));
    float onBase = step(0.62, seed);
    float flicker = 0.88 + 0.12 * sin(t * (1.6 + seed * 5.0) + seed * 20.0);
    float light = onBase * flicker;

    // Hover floor: brighten the whole floor
    float hoverFloor = 0.0;

    if (enableHoverFloor)
    {
        float mouseWorldX = m.x + scroll;
        float mgx = floor(mouseWorldX / colW);

        if (abs(mgx - gx) < 0.5)
        {
            float mfx = fract(mouseWorldX / colW) - 0.5;
            if (abs(mfx) <= bw)
            {
                float myLocalY = (m.y - (-1.0)) / (h + 1e-6);
                if (myLocalY >= 0.0 && myLocalY <= 1.0 && m.y <= yTop + 0.02)
                {
                    float floorId = floor(myLocalY * winRows);
                    float sameFloor = 1.0 - step(0.5, abs(wid.y - floorId));

                    // "whole floor" base boost
                    hoverFloor = sameFloor;
                }
            }
        }
    }

    // lighting strength
    float lit = light;
    lit += hoverFloor * 0.9;      // entire floor brighter
    lit = clamp(lit, 0.0, 2.5);

    // glow around hover floor (soft)
    float glow = hoverFloor * 0.25 * isWindow;

    vec3 winCol = warmLight * (0.12 + 0.95 * clamp(lit, 0.0, 1.0));
    winCol += vec3(0.45, 0.15, 0.70) * (0.35 * glow); // purple glow

    // compose building color: base body + windows (only where isWindow)
    vec3 rgb = bCol;
    rgb = mix(rgb, winCol, isWindow * clamp(lit, 0.0, 1.0));

    // extra additive glow inside body
    rgb += warmLight * (0.08 * glow) * body;

    LayerOut outp;
    outp.rgb = rgb;
    outp.a = alpha;
    return outp;
}

void main()
{
    vec2 res = max(u_resolution, vec2(1.0));
    vec2 suv = gl_FragCoord.xy / res;
    vec2 p = suv * 2.0 - 1.0;
    p.x *= res.x / res.y;

    vec2 m = (u_mouse * 2.0 - 1.0);
    m.x *= res.x / res.y;

    float t = u_time;

    float y01 = clamp((p.y + 1.0) * 0.5, 0.0, 1.0);
    vec3 col = skyColor(y01, p, t, u_skyMode);

    // stars (night/purple)
    if (u_skyMode != 1) {
        float star = step(0.9972, hash21(floor(suv * vec2(res.x, res.y) * 0.35)));
        col += star * vec3(0.7, 0.6, 0.9) * smoothstep(0.25, 1.0, y01);
    }

    // BACK layer: slower + hazier
    LayerOut back = buildingsLayer(
        p, m, t,
        -0.02,
        0.10,
        0.65,
        vec2(0.16, 0.24),
        vec2(0.40, 1.10),
        3.0, 5.0,
        7.0, 12.0,
        vec3(0.07, 0.05, 0.10),
        vec3(0.12, 0.09, 0.16),
        vec3(0.10, 0.06, 0.18),
        vec3(0.85, 0.72, 0.45),
        false
    );

    // haze tint for back
    vec3 hazeTint = (u_skyMode == 1) ? vec3(0.90, 0.95, 1.00) :
                   (u_skyMode == 0) ? vec3(0.08, 0.10, 0.18) :
                                      vec3(0.35, 0.18, 0.55);
    back.rgb = mix(back.rgb, back.rgb + hazeTint * 0.10, 0.65);

    // Proper blend (back behind sky)
    col = mix(col, back.rgb, back.a);

    // FRONT layer: faster + bigger + hover-floor ripple enabled
    LayerOut front = buildingsLayer(
        p, m, t,
        -0.14,
        0.22,
        0.95,                 // bigger columns -> bigger obvious buildings
        vec2(0.28, 0.40),      // wider building body -> bigger
        vec2(0.80, 1.85),
        2.5, 4.0,
        6.0, 11.0,
        vec3(0.05, 0.03, 0.08),
        vec3(0.10, 0.06, 0.14),
        vec3(0.14, 0.07, 0.24),
        vec3(1.00, 0.82, 0.45),
        true
    );

    // FRONT must occlude BACK (this is the fix you asked)
    col = mix(col, front.rgb, front.a);

    // (removed vignette entirely — no black mask around edges)

    FragColor = vec4(col, 1.0);
}
