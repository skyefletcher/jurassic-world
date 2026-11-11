shader_type spatial;
render_mode cull_disabled, blend_mix, depth_prepass_alpha;
void fragment() {
    float streak = sin(UV.x * 92.0 + sin(UV.y * 17.0 - TIME * 5.0)) * 0.5 + 0.5;
    float foam = smoothstep(0.3, 0.95, sin(UV.y * 65.0 - TIME * 12.0) * 0.5 + 0.5);
    ALBEDO = mix(vec3(0.18, 0.55, 0.54), vec3(0.88, 0.97, 0.92), streak * 0.4 + foam * 0.4);
    ROUGHNESS = 0.2;
    ALPHA = 0.88;
}
