import type { Transition } from 'motion/react'

export const motionTransition = {
  duration: 0.18,
  ease: [0.2, 0, 0, 1],
} satisfies Transition

export const surfaceEnter = {
  initial: { opacity: 0, y: 6 },
  animate: { opacity: 1, y: 0 },
  transition: motionTransition,
} as const
