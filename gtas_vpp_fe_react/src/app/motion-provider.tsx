import { domAnimation, LazyMotion, MotionConfig } from 'motion/react'
import type { PropsWithChildren } from 'react'

import { motionTransition } from '@/lib/motion'

export function AppMotionProvider({ children }: PropsWithChildren) {
  return (
    <MotionConfig reducedMotion="user" transition={motionTransition}>
      <LazyMotion features={domAnimation} strict>
        {children}
      </LazyMotion>
    </MotionConfig>
  )
}
