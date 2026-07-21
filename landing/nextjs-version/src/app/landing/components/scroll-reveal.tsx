"use client"

import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react'

type ScrollRevealProps = {
  children: ReactNode
}

const ITEM_TRANSITION_DURATION_MS = 700
const ITEM_OVERLAP_DELAY_MS = ITEM_TRANSITION_DURATION_MS / 2

export function ScrollReveal({ children }: ScrollRevealProps) {
  const elementRef = useRef<HTMLDivElement>(null)
  const [isWaitingToReveal, setIsWaitingToReveal] = useState(false)
  const [isVisible, setIsVisible] = useState(false)
  const itemQueueRef = useRef<HTMLElement[]>([])

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

    itemQueueRef.current = Array.from(
      element.querySelectorAll<HTMLElement>('[data-scroll-reveal-item]'),
    ).sort((first, second) => {
      const firstPosition = first.getBoundingClientRect()
      const secondPosition = second.getBoundingClientRect()

      if (Math.abs(firstPosition.top - secondPosition.top) > 8) {
        return firstPosition.top - secondPosition.top
      }

      return firstPosition.left - secondPosition.left
    })

    itemQueueRef.current.forEach((item) => item.classList.add('scroll-reveal-item', 'scroll-reveal-item-hidden'))
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

  useEffect(() => {
    const element = elementRef.current

    if (!element || !isVisible || itemQueueRef.current.length === 0) {
      return
    }

    let currentItemIndex = 0
    let nextItemTimeout: ReturnType<typeof window.setTimeout> | undefined

    const revealNextItem = () => {
      const item = itemQueueRef.current[currentItemIndex]
      currentItemIndex += 1

      if (item) {
        item.classList.remove('scroll-reveal-item-hidden')
        nextItemTimeout = window.setTimeout(revealNextItem, ITEM_OVERLAP_DELAY_MS)
      }
    }

    // A sequência interna começa no momento em que esta seção entra na viewport.
    // Ela não depende da conclusão de nenhuma outra seção da página.
    revealNextItem()

    return () => {
      if (nextItemTimeout) window.clearTimeout(nextItemTimeout)
    }
  }, [isVisible])

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
