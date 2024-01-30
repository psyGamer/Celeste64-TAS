# Shaders

When working with shaders, you need to pay attention to 1 important thing: 
The GPU **aggressively** strips out _everything_ which isn't actually used.

That means than even if a uniform is used in the vertex shader, if the data depending on that uniform isn't actually used,
it'll get stripped. 

This can cause some very weird issues!

For example `o_color = vec4(1.0);` will discard a lot of uniforms, so be careful with that!

One way to avoid issues while testing is to _technically_ still use everything: `o_color = mix(vec4(1), o_color, 0.000000001);`

This uses the previous `o_color` (which will depend on everything important) and mixes it with your new color in a way which cant' be seen.
Of course, after testing that should be removed, tho in some cases that may not be possible.