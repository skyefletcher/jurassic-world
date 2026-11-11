shader_type spatial;

uniform vec4 hide_colour : source_color = vec4(0.34, 0.4, 0.28, 1.0);
uniform float body_height = 2.0;
uniform bool raptor = false;
uniform int raptor_variant = 0;
uniform mat4 world_to_animal;
varying vec3 hide_position;
varying vec3 hide_normal;

void vertex() {
    hide_position = (world_to_animal * MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz / body_height;
    hide_normal = normalize(mat3(world_to_animal) * mat3(MODEL_MATRIX) * NORMAL);
}

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

// Staggered, rounded polygon scales. Cell size varies gently rather than forming a checkerboard.
vec2 scutes(vec2 p) {
    p *= 115.0;
    float row = floor(p.y);
    p.x += mod(row, 2.0) * 0.5;
    vec2 cell = floor(p);
    vec2 uv = fract(p) - 0.5;
    float h = hash(cell);
    float edge = max(abs(uv.x) * 0.866 + abs(uv.y) * 0.5, abs(uv.y));
    float aa = max(fwidth(edge) * 0.6, 0.012);
    float ridge = 1.0 - smoothstep(0.34 + h * 0.025 - aa, 0.49 + aa, edge);
    float footprint = max(length(dFdx(p)), length(dFdy(p)));
    float detail = 1.0 - smoothstep(0.55, 2.2, footprint);
    return mix(vec2(0.72, 0.5), vec2(ridge, h), detail);
}

void fragment() {
    vec3 weights = pow(abs(normalize(hide_normal)), vec3(5.0));
    weights /= max(dot(weights, vec3(1.0)), 0.001);
    vec2 scales = scutes(hide_position.yz) * weights.x + scutes(hide_position.xz) * weights.y + scutes(hide_position.xy) * weights.z;
    float mottles = sin(hide_position.z * 21.0 + sin(hide_position.x * 33.0)) * sin(hide_position.y * 37.0 + hide_position.z * 9.0);
    vec3 colour = hide_colour.rgb * (0.84 + scales.x * 0.12 + scales.y * 0.055 + mottles * 0.06);
    // Subtle countershading follows the surface orientation, including the neck and lower jaw.
    float underside = smoothstep(0.05, 0.85, -normalize(hide_normal).y);
    colour = mix(colour, hide_colour.rgb * 1.22 + vec3(0.07, 0.065, 0.035), underside * 0.62);
    float back_bars = smoothstep(0.55, 0.95, sin(hide_position.z * 39.0 + sin(hide_position.x * 13.0))) * smoothstep(-0.05, 0.45, normalize(hide_normal).y);
    colour *= 1.0 - back_bars * 0.18;
    if (raptor) {
        float flank = smoothstep(0.4, 0.8, abs(normalize(hide_normal).x));
        float stripe_height = 0.56 + 0.23 * smoothstep(0.18, 0.68, -hide_position.z);
        float line = abs(hide_position.y - stripe_height - 0.012 * sin(hide_position.z * 13.0));
        float outline = (1.0 - smoothstep(0.035, 0.048, line)) * flank;
        float stripe = (1.0 - smoothstep(0.019, 0.032, line)) * flank;
        vec3 mark = raptor_variant == 0 ? vec3(0.06, 0.22, 0.38) : vec3(0.11, 0.17, 0.10);
        colour = mix(colour, vec3(0.59, 0.61, 0.52) * (0.83 + scales.x * 0.17), outline * 0.75);
        colour = mix(colour, mark * (0.75 + scales.x * 0.25), stripe * 0.95);
        if (raptor_variant == 2) colour *= 1.0 - smoothstep(0.3, 0.8, sin(hide_position.z * 48.0 + hide_position.y * 8.0)) * flank * 0.38;
    }
    ALBEDO = colour;
    ROUGHNESS = 0.76 + scales.y * 0.18;
    // Screen derivatives turn the scale relief into real lighting detail, without external texture files.
    float relief = scales.x * 0.00014 * body_height;
    vec3 dx = dFdx(VERTEX), dy = dFdy(VERTEX);
    vec3 r1 = cross(dy, NORMAL), r2 = cross(NORMAL, dx);
    float det = dot(dx, r1);
    NORMAL = normalize(abs(det) * NORMAL - sign(det) * (dFdx(relief) * r1 + dFdy(relief) * r2));
}
