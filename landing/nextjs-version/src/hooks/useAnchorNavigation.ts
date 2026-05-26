import { useRouter, usePathname } from 'next/navigation'

export function useAnchorNavigation() {
  const router = useRouter()
  const pathname = usePathname()

  const navigateToSection = (sectionId: string) => {
    if (pathname === '/') {
      // Se já está na home, apenas rola para a seção
      const element = document.getElementById(sectionId)
      if (element) {
        element.scrollIntoView({ behavior: 'smooth' })
      }
    } else {
      // Se não está na home, navega para lá e depois rola
      router.push(`/#${sectionId}`)
      
      // Aguarda o carregamento da página e rola para a seção
      setTimeout(() => {
        const element = document.getElementById(sectionId)
        if (element) {
          element.scrollIntoView({ behavior: 'smooth' })
        }
      }, 100)
    }
  }

  return { navigateToSection }
}
