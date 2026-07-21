"use client"

import { useLayoutEffect, useRef, useState, type ReactNode } from 'react'

type ScrollRevealProps = {
  children: ReactNode
}

export function ScrollReveal({ children }: ScrollRevealProps) {
  const elementRef = useRef<HTMLDivElement>(null)
  const [isWaitingToReveal, setIsWaitingToReveal] = useState(false)
  const [isVisible, setIsVisible] = useState(false)

  useLayoutEffect(() => {
    const element = elementRef.current

    if (!element || window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return
    }

    // Sections already visible on page load should remain visible; only sections
    // below the fold begin hidden and animate as the visitor scrolls to them.
    if (element.getBoundingClientRect().top <= window.innerHeight * 0.85) {
      setIsVisible(true)
      return
    }

    setIsWaitingToReveal(true)

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) {
          return
        }

        setIsVisible(true)
        observer.unobserve(element)
      },
      { threshold: 0.12, rootMargin: '0px 0px -8% 0px' },
    )

    observer.observe(element)

    return () => observer.disconnect()
  }, [])

  return (
    <div
      ref={elementRef}
      className={isWaitingToReveal && !isVisible
        ? 'translate-y-8 opacity-0 transition-[opacity,transform] duration-700 ease-out motion-reduce:translate-y-0 motion-reduce:transition-none'
        : 'translate-y-0 opacity-100 transition-[opacity,transform] duration-700 ease-out motion-reduce:transition-none'}
    >
      {children}
    </div>
  )
}
