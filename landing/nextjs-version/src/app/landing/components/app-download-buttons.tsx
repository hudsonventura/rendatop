'use client'

import { APP_CONFIG } from '@/config/app';
import React from 'react';

export default function AppDownloadButtons() {
  const googlePlayUrl = process.env.NEXT_PUBLIC_APPS_GOOGLE_PLAY_URL || 'https://play.google.com/store'
  const googlePlayActive = !!process.env.NEXT_PUBLIC_APPS_GOOGLE_PLAY_URL
  const appStoreUrl = process.env.NEXT_PUBLIC_APPS_APP_STORE_URL || 'https://www.apple.com/app-store'
const appStoreActive = !!process.env.NEXT_PUBLIC_APPS_APP_STORE_URL 

    const clientBaseUrl = APP_CONFIG.BASE_URL

  return (
    <div id="downloads" className="flex flex-col sm:flex-row gap-4 items-center justify-center p-6 bg-background">
        <section id="about" className="py-24 sm:py-32">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div data-scroll-reveal-item className="mx-auto max-w-4xl text-center mb-16">
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-6">
            No app ou na web você encontra o melhor investimento
          </h2>
          <p className="text-lg text-muted-foreground mb-8">
            Veja na web ou no celular, como preferir, o Rendatop é acessível onde você estiver. Baixe nosso aplicativo para uma experiência otimizada ou acesse diretamente pelo navegador. Estamos aqui para facilitar sua vida financeira, seja qual for a plataforma que você escolher.
          </p>
        </div>

        <div className="flex justify-center items-center flex-col sm:flex-row gap-4">
            
            
                <a
                    data-scroll-reveal-item
                    href={googlePlayUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-3 bg-black text-white px-5 py-2.5 rounded-xl border border-neutral-800 hover:bg-neutral-900 transition-colors duration-200 min-w-[200px] select-none shadow-sm"
                >
                    {/* Google Play SVG Icon */}
                <svg
                aria-hidden="true"
                viewBox="0 0 512 512"
                className="w-7 h-7"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                >
                <path
                    d="M32.5 17.5C30.4 19.7 29.2 23.1 29.2 27.4V484.7C29.2 489 30.4 492.4 32.5 494.6L34.1 496.1L271.7 258.5V253.6L34.1 16L32.5 17.5Z"
                    fill="url(#play_gradient_1)"
                />
                <path
                    d="M350.7 337.6L271.7 258.5V253.6L350.7 174.5L352.4 175.5L446 228.7C472.7 243.9 472.7 268.2 446 283.4L352.4 336.6L350.7 337.6Z"
                    fill="url(#play_gradient_2)"
                />
                <path
                    d="M352.4 175.5L271.7 256.1L34.1 18.5C37.5 14.9 44.4 12.8 52.8 17.5L352.4 175.5Z"
                    fill="url(#play_gradient_3)"
                />
                <path
                    d="M352.4 336.6L52.8 506.6C44.4 511.3 37.5 509.2 34.1 505.6L271.7 258.5L352.4 336.6Z"
                    fill="url(#play_gradient_4)"
                />
                <defs>
                    <linearGradient id="play_gradient_1" x1="25.5" y1="256" x2="242.3" y2="256" gradientUnits="userSpaceOnUse">
                    <stop offset="0" stopColor="#00A0FF" />
                    <stop offset="0.01" stopColor="#00A1FF" />
                    <stop offset="0.26" stopColor="#00BEFF" />
                    <stop offset="0.51" stopColor="#00D2FF" />
                    <stop offset="0.76" stopColor="#00DEFF" />
                    <stop offset="1" stopColor="#00E2FF" />
                    </linearGradient>
                    <linearGradient id="play_gradient_2" x1="482.3" y1="256" x2="255.4" y2="256" gradientUnits="userSpaceOnUse">
                    <stop offset="0" stopColor="#FFE000" />
                    <stop offset="0.4" stopColor="#FFCA00" />
                    <stop offset="0.78" stopColor="#FFB300" />
                    <stop offset="1" stopColor="#FFA500" />
                    </linearGradient>
                    <linearGradient id="play_gradient_3" x1="168" y1="120.3" x2="310.2" y2="-21.9" gradientUnits="userSpaceOnUse">
                    <stop offset="0" stopColor="#FF0A00" />
                    <stop offset="0.23" stopColor="#FF2300" />
                    <stop offset="0.63" stopColor="#FF5600" />
                    <stop offset="0.94" stopColor="#FF7700" />
                    <stop offset="1" stopColor="#FF8000" />
                    </linearGradient>
                    <linearGradient id="play_gradient_4" x1="110.1" y1="363.1" x2="271.7" y2="524.7" gradientUnits="userSpaceOnUse">
                    <stop offset="0" stopColor="#00F076" />
                    <stop offset="0.45" stopColor="#00CD66" />
                    <stop offset="0.8" stopColor="#01A551" />
                    <stop offset="1" stopColor="#008744" />
                    </linearGradient>
                </defs>
                </svg>

                {/* Text Container */}
                <div className="flex flex-col items-start leading-none">
                <span className="text-[10px] font-medium tracking-wider text-neutral-400 uppercase">
                    Get it on
                </span>
                <span className="text-[19px] font-semibold text-white mt-1 font-sans tracking-tight">
                    {!googlePlayActive && ("Em breve na ")} Google Play
                </span>
                </div>
            </a>
           
            
            {/* Apple App Store Button */}
            <a
                data-scroll-reveal-item
                href={appStoreUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-3 bg-black text-white px-5 py-2.5 rounded-xl border border-neutral-800 hover:bg-neutral-900 transition-colors duration-200 min-w-[200px] select-none shadow-sm"
            >
                {/* Apple SVG Icon */}
                <svg
                aria-hidden="true"
                viewBox="0 0 384 512"
                className="w-7 h-7"
                fill="currentColor"
                xmlns="http://www.w3.org/2000/svg"
                >
                <path d="M318.7 268.7c-.2-36.7 16.4-64.4 50-84.8-18.8-26.9-47.2-41.7-84.7-44.6-35.5-2.8-74.3 20.7-88.5 20.7-15 0-49.4-19.7-76.4-19.7C63.3 141.2 4 184.8 4 273.5q0 39.3 14.4 81.2c12.8 36.7 59 126.7 107.2 125.2 25.2-.6 43-17.9 75.8-17.9 31.8 0 48.3 17.9 76.4 17.9 48.6-.7 90.4-82.5 102.6-119.3-65.2-30.7-61.7-90-61.7-91.9zm-56.6-164.2c27.3-32.4 24.8-61.9 24-72.5-24.1 1.4-52 16.4-67.9 34.9-17.5 19.8-27.8 47.5-24.4 76.5 26.9 2.4 51.2-16 68.3-38.9z" />
                </svg>

                {/* Text Container */}
                <div className="flex flex-col items-start leading-none">
                <span className="text-[10px] font-medium tracking-wide text-neutral-400">
                    Download on the
                </span>
                <span className="text-[19px] font-semibold text-white mt-0.5 font-sans tracking-tight">
                    {!appStoreActive && ("Em breve na ")} App Store
                </span>
                </div>
            </a>

            {/* Web Interface Button */}
            <a
                data-scroll-reveal-item
                href={clientBaseUrl+'/login'}
                className="inline-flex items-center gap-3 bg-black text-white px-5 py-2.5 rounded-xl border border-neutral-800 hover:bg-neutral-900 transition-colors duration-200 min-w-[200px] select-none shadow-sm"
            >
                {/* Browser SVG Icon */}
                <svg
                aria-hidden="true"
                viewBox="0 0 24 24"
                className="w-7 h-7"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                xmlns="http://www.w3.org/2000/svg"
                >
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
                <path d="M2 17h20" />
                <path d="M6 17v2" />
                <path d="M18 17v2" />
                </svg>

                {/* Text Container */}
                <div className="flex flex-col items-start leading-none">
                <span className="text-[10px] font-medium tracking-wider text-neutral-400 uppercase">
                    Access in
                </span>
                <span className="text-[19px] font-semibold text-white mt-1 font-sans tracking-tight">
                    Web
                </span>
                </div>
            </a>
        </div>
            

        
      </div>
    </section>
      
    </div>
  );
}
