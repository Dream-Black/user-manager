<template>
  <canvas ref="canvasRef" class="three-background"></canvas>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import * as THREE from 'three'

const canvasRef = ref(null)
let animationId = null
let scene, camera, renderer, particles, mouse
let particleCount = 150

const mousePosition = { x: 0, y: 0 }

onMounted(() => {
  initThree()
  animate()
  window.addEventListener('mousemove', handleMouseMove)
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('mousemove', handleMouseMove)
  window.removeEventListener('resize', handleResize)
  if (animationId) {
    cancelAnimationFrame(animationId)
  }
  if (renderer) {
    renderer.dispose()
  }
})

const handleMouseMove = (event) => {
  mousePosition.x = (event.clientX / window.innerWidth) * 2 - 1
  mousePosition.y = -(event.clientY / window.innerHeight) * 2 + 1
}

const handleResize = () => {
  if (!camera || !renderer) return
  camera.aspect = window.innerWidth / window.innerHeight
  camera.updateProjectionMatrix()
  renderer.setSize(window.innerWidth, window.innerHeight)
}

const initThree = () => {
  const canvas = canvasRef.value
  if (!canvas) return

  // 场景
  scene = new THREE.Scene()

  // 相机
  camera = new THREE.PerspectiveCamera(
    75,
    window.innerWidth / window.innerHeight,
    0.1,
    1000
  )
  camera.position.z = 50

  // 渲染器
  renderer = new THREE.WebGLRenderer({
    canvas,
    alpha: true,
    antialias: true
  })
  renderer.setSize(window.innerWidth, window.innerHeight)
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
  renderer.setClearColor(0x000000, 0)

  // 创建粒子几何体
  const geometry = new THREE.BufferGeometry()
  const positions = new Float32Array(particleCount * 3)
  const colors = new Float32Array(particleCount * 3)
  const sizes = new Float32Array(particleCount)

  // 浅色系配色
  const colorPalette = [
    new THREE.Color(0x4a90d9), // 蓝色
    new THREE.Color(0x7B61FF), // 紫色
    new THREE.Color(0x63b3ed), // 浅蓝
    new THREE.Color(0x90cdf4), // 更浅蓝
    new THREE.Color(0xb794f4), // 浅紫
  ]

  for (let i = 0; i < particleCount; i++) {
    // 位置 - 分布在3D空间
    positions[i * 3] = (Math.random() - 0.5) * 100
    positions[i * 3 + 1] = (Math.random() - 0.5) * 100
    positions[i * 3 + 2] = (Math.random() - 0.5) * 50

    // 颜色
    const color = colorPalette[Math.floor(Math.random() * colorPalette.length)]
    colors[i * 3] = color.r
    colors[i * 3 + 1] = color.g
    colors[i * 3 + 2] = color.b

    // 大小
    sizes[i] = Math.random() * 3 + 1
  }

  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3))
  geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3))
  geometry.setAttribute('size', new THREE.BufferAttribute(sizes, 1))

  // 创建自定义着色器材质
  const vertexShader = `
    attribute float size;
    attribute vec3 color;
    varying vec3 vColor;
    uniform float time;

    void main() {
      vColor = color;
      vec3 pos = position;

      // 添加轻微的浮动动画
      pos.y += sin(time * 0.5 + position.x * 0.1) * 2.0;
      pos.x += cos(time * 0.3 + position.y * 0.1) * 1.5;

      vec4 mvPosition = modelViewMatrix * vec4(pos, 1.0);
      gl_PointSize = size * (300.0 / -mvPosition.z);
      gl_Position = projectionMatrix * mvPosition;
    }
  `

  const fragmentShader = `
    varying vec3 vColor;
    uniform float time;

    void main() {
      // 创建圆形粒子
      float dist = length(gl_PointCoord - vec2(0.5));
      if (dist > 0.5) discard;

      // 添加发光效果
      float alpha = 1.0 - smoothstep(0.3, 0.5, dist);
      alpha *= 0.6 + sin(time + gl_PointCoord.x * 10.0) * 0.1;

      gl_FragColor = vec4(vColor, alpha * 0.7);
    }
  `

  const material = new THREE.ShaderMaterial({
    uniforms: {
      time: { value: 0 }
    },
    vertexShader,
    fragmentShader,
    transparent: true,
    depthWrite: false,
    blending: THREE.AdditiveBlending
  })

  particles = new THREE.Points(geometry, material)
  scene.add(particles)

  // 添加一些线条连接近的粒子
  const lineGeometry = new THREE.BufferGeometry()
  const linePositions = new Float32Array(particleCount * 3 * 2)
  lineGeometry.setAttribute('position', new THREE.BufferAttribute(linePositions, 3))

  const lineMaterial = new THREE.LineBasicMaterial({
    color: 0x4a90d9,
    transparent: true,
    opacity: 0.05,
    blending: THREE.AdditiveBlending
  })

  const lines = new THREE.LineSegments(lineGeometry, lineMaterial)
  scene.add(lines)

  // 存储线条对象供动画使用
  scene.userData.lines = lines
  scene.userData.linePositions = linePositions
}

const animate = () => {
  animationId = requestAnimationFrame(animate)

  if (!particles || !particles.material) return

  const time = Date.now() * 0.001
  particles.material.uniforms.time.value = time

  // 鼠标交互：粒子轻微跟随鼠标
  particles.rotation.y += (mousePosition.x * 0.5 - particles.rotation.y) * 0.02
  particles.rotation.x += (mousePosition.y * 0.5 - particles.rotation.x) * 0.02

  // 更新连线
  const positions = particles.geometry.attributes.position.array
  const linePositions = scene.userData.linePositions
  const lines = scene.userData.lines

  if (lines && linePositions) {
    let lineIndex = 0
    const maxDistance = 15

    for (let i = 0; i < particleCount; i++) {
      for (let j = i + 1; j < particleCount; j++) {
        const dx = positions[i * 3] - positions[j * 3]
        const dy = positions[i * 3 + 1] - positions[j * 3 + 1]
        const dz = positions[i * 3 + 2] - positions[j * 3 + 2]
        const distance = Math.sqrt(dx * dx + dy * dy + dz * dz)

        if (distance < maxDistance && lineIndex < particleCount * 2 - 2) {
          linePositions[lineIndex++] = positions[i * 3]
          linePositions[lineIndex++] = positions[i * 3 + 1]
          linePositions[lineIndex++] = positions[i * 3 + 2]

          linePositions[lineIndex++] = positions[j * 3]
          linePositions[lineIndex++] = positions[j * 3 + 1]
          linePositions[lineIndex++] = positions[j * 3 + 2]
        }
      }
    }

    lines.geometry.setAttribute(
      'position',
      new THREE.BufferAttribute(linePositions.slice(0, lineIndex), 3)
    )
    lines.geometry.attributes.position.needsUpdate = true
  }

  renderer.render(scene, camera)
}
</script>

<style scoped>
.three-background {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 0;
  pointer-events: none;
}
</style>
