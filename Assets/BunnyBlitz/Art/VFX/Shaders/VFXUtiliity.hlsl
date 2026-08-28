/// zOffset: An offset applied to the Z position.
void ZFlatten(inout VFXAttributes attributes, in float zOffset)
{
  //attributes.position.z = TransformPositionVFXToWorld(float3(0,0,0)).z - zOffset;
  attributes.position.z -= zOffset;
}