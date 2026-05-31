/**
 * Generates Assets/_Animations/Enemies/DragonAttack.anim (bite / breath lunge).
 * Run: node tools/generateDragonAttackAnim.js
 */
const fs = require('fs');
const path = require('path');

const GLB_PATH = path.join(__dirname, '../Assets/dragon/upload/Dragon2.glb');
const OUT_PATH = path.join(__dirname, '../Assets/_Animations/Enemies/DragonAttack.anim');
const DURATION = 1;

function loadGlb(filePath) {
  const buf = fs.readFileSync(filePath);
  const jsonLen = buf.readUInt32LE(12);
  return JSON.parse(buf.toString('utf8', 20, 20 + jsonLen));
}

function quatMul(a, b) {
  return {
    x: a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
    y: a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
    z: a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
    w: a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z,
  };
}

function quatNorm(q) {
  const m = Math.hypot(q.x, q.y, q.z, q.w) || 1;
  return { x: q.x / m, y: q.y / m, z: q.z / m, w: q.w / m };
}

function quatFromEuler(degX, degY, degZ) {
  const toR = (d) => (d * Math.PI) / 180;
  const ex = toR(degX) * 0.5;
  const ey = toR(degY) * 0.5;
  const ez = toR(degZ) * 0.5;
  const cx = Math.cos(ex);
  const sx = Math.sin(ex);
  const cy = Math.cos(ey);
  const sy = Math.sin(ey);
  const cz = Math.cos(ez);
  const sz = Math.sin(ez);
  return quatNorm({
    x: sx * cy * cz - cx * sy * sz,
    y: cx * sy * cz + sx * cy * sz,
    z: cx * cy * sz - sx * sy * cz,
    w: cx * cy * cz + sx * sy * sz,
  });
}

function fmt(n) {
  const v = Object.is(n, -0) ? 0 : n;
  const s = v.toFixed(8).replace(/\.?0+$/, '');
  if (!s.includes('.')) return s + '.0';
  return s;
}

function fmtQuat(q) {
  return `{x: ${fmt(q.x)}, y: ${fmt(q.y)}, z: ${fmt(q.z)}, w: ${fmt(q.w)}}`;
}

function keyframeBlock(time, q) {
  return `      - serializedVersion: 3
        time: ${time}
        value: ${fmtQuat(q)}
        inSlope: {x: 0, y: 0, z: 0, w: 0}
        outSlope: {x: 0, y: 0, z: 0, w: 0}
        tangentMode: 0
        weightedMode: 0
        inWeight: {x: 0.33333334, y: 0.33333334, z: 0.33333334, w: 0.33333334}
        outWeight: {x: 0.33333334, y: 0.33333334, z: 0.33333334, w: 0.33333334}`;
}

function rotationCurve(path, keyframes, bindQuat) {
  const keys = keyframes.map((kf) => {
    const offset = quatFromEuler(kf.euler[0], kf.euler[1], kf.euler[2]);
    const value = quatNorm(quatMul(bindQuat, offset));
    return { time: kf.t, value };
  });
  const lines = keys.map((k) => keyframeBlock(k.time, k.value)).join('\n');
  return `  - curve:
      serializedVersion: 2
      m_Curve:
${lines}
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
    path: ${path}`;
}

const glb = loadGlb(GLB_PATH);
const pathByName = new Map();

function walk(i, prefix) {
  const n = glb.nodes[i];
  const name = n.name || `n${i}`;
  const full = prefix ? `${prefix}/${name}` : name;
  if (prefix) pathByName.set(name, full);
  (n.children || []).forEach((c) => walk(c, full));
}
walk(77, '');

function bindQuat(name) {
  const i = glb.nodes.findIndex((n) => n.name === name);
  const r = glb.nodes[i].rotation;
  if (!r) return { x: 0, y: 0, z: 0, w: 1 };
  return quatNorm({ x: r[0], y: r[1], z: r[2], w: r[3] });
}

// Bite only: head + jaw chain. Wings, legs, tail, torso stay at default pose.
const boneTracks = [
  {
    name: 'Head',
    keys: [
      { t: 0, euler: [0, 0, 0] },
      { t: 0.1, euler: [-16, 0, 0] },
      { t: 0.24, euler: [28, 0, 0] },
      { t: 0.36, euler: [32, 0, 0] },
      { t: DURATION, euler: [0, 0, 0] },
    ],
  },
  {
    name: 'Head.001',
    keys: [
      { t: 0, euler: [0, 0, 0] },
      { t: 0.12, euler: [-10, 0, 0] },
      { t: 0.26, euler: [22, 0, 0] },
      { t: DURATION, euler: [0, 0, 0] },
    ],
  },
  {
    name: 'Head.002',
    keys: [
      { t: 0, euler: [0, 0, 0] },
      { t: 0.2, euler: [4, 0, 0] },
      { t: 0.3, euler: [45, 0, 0] },
      { t: 0.38, euler: [10, 0, 0] },
      { t: 0.5, euler: [16, 0, 0] },
      { t: DURATION, euler: [0, 0, 0] },
    ],
  },
  {
    name: 'Head.003',
    keys: [
      { t: 0, euler: [0, 0, 0] },
      { t: 0.28, euler: [18, 0, 0] },
      { t: 0.38, euler: [-10, 0, 0] },
      { t: DURATION, euler: [0, 0, 0] },
    ],
  },
  {
    name: 'Head.downTip',
    keys: [
      { t: 0, euler: [0, 0, 0] },
      { t: 0.3, euler: [32, 0, 0] },
      { t: 0.4, euler: [-6, 0, 0] },
      { t: DURATION, euler: [0, 0, 0] },
    ],
  },
];

const rotationCurves = boneTracks
  .map((track) => {
    const bonePath = pathByName.get(track.name);
    if (!bonePath) {
      console.warn('Missing bone:', track.name);
      return null;
    }
    return rotationCurve(bonePath, track.keys, bindQuat(track.name));
  })
  .filter(Boolean)
  .join('\n');

const yaml = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: DragonAttack
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves:
${rotationCurves}
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves: []
  m_SampleRate: 60
  m_WrapMode: 0
  m_Bounds:
    m_Center: {x: 0, y: 0, z: 0}
    m_Extent: {x: 0, y: 0, z: 0}
  m_ClipBindingConstant:
    genericBindings: []
    pptrCurveMapping: []
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: ${DURATION}
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 0
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
`;

fs.writeFileSync(OUT_PATH, yaml);
console.log('Wrote', OUT_PATH, 'with', boneTracks.length, 'bone tracks,', DURATION, 'seconds');
