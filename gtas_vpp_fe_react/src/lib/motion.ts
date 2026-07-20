import type { Transition } from 'motion/react'

export const motionTransition = {
  duration: 0.18,
  ease: [0.2, 0, 0, 1],
} satisfies Transition

export const surfaceEnter = {
  // Keep readable content fully opaque during accessibility sampling.  The
  // surface still gets a subtle transform-only entrance motion.
  initial: { y: 6, scale: 0.99 },
  animate: { y: 0, scale: 1 },
  transition: motionTransition,
} as const

// Route-level motion chỉ dùng transform để không làm thay đổi độ tương phản
// trong lúc trình đọc màn hình/axe kiểm tra nội dung đang hiển thị.
export const routeEnter = {
  initial: { y: 4 },
  animate: { y: 0 },
  transition: motionTransition,
} as const
