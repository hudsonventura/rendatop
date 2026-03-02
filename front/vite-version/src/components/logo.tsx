import * as React from "react"

interface LogoProps extends React.SVGProps<SVGSVGElement> {
  size?: number
}

export function Logo({ size = 48, className, ...props }: LogoProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 100 100"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      {...props}
    >
      {/* Back bill (rotated) */}
      <g transform="rotate(-15, 50, 45)">
        <rect x="8" y="20" width="70" height="42" rx="4" fill="#4CAF50" />
        <rect x="8" y="20" width="70" height="42" rx="4" stroke="#388E3C" strokeWidth="1.5" />
        {/* Oval highlight on back bill */}
        <ellipse cx="43" cy="41" rx="12" ry="12" fill="#388E3C" />
        <ellipse cx="43" cy="41" rx="9" ry="9" fill="#4CAF50" stroke="#388E3C" strokeWidth="1.5" />
        {/* Dollar sign on back bill */}
        <text x="43" y="46" textAnchor="middle" fill="#388E3C" fontSize="11" fontWeight="bold" fontFamily="sans-serif">$</text>
        {/* Corner decorations */}
        <rect x="12" y="24" width="10" height="6" rx="1" fill="#388E3C" opacity="0.5" />
        <rect x="62" y="50" width="10" height="6" rx="1" fill="#388E3C" opacity="0.5" />
      </g>

      {/* Front bill */}
      <rect x="14" y="26" width="70" height="42" rx="4" fill="#66BB6A" />
      <rect x="14" y="26" width="70" height="42" rx="4" stroke="#43A047" strokeWidth="1.5" />
      {/* Oval highlight on front bill */}
      <ellipse cx="49" cy="47" rx="13" ry="13" fill="#43A047" />
      <ellipse cx="49" cy="47" rx="10" ry="10" fill="#66BB6A" stroke="#43A047" strokeWidth="1.5" />
      {/* Dollar sign on front bill */}
      <text x="49" y="52" textAnchor="middle" fill="#43A047" fontSize="13" fontWeight="bold" fontFamily="sans-serif">$</text>
      {/* Corner decorations */}
      <rect x="18" y="30" width="12" height="7" rx="1" fill="#43A047" opacity="0.5" />
      <rect x="68" y="57" width="12" height="7" rx="1" fill="#43A047" opacity="0.5" />

      {/* Coin (back) */}
      <ellipse cx="68" cy="78" rx="13" ry="13" fill="#FFD54F" />
      <ellipse cx="68" cy="78" rx="13" ry="13" stroke="#F9A825" strokeWidth="1.5" />
      <ellipse cx="68" cy="78" rx="9" ry="9" fill="#FFD54F" stroke="#F9A825" strokeWidth="1" />
      <text x="68" y="82" textAnchor="middle" fill="#F9A825" fontSize="10" fontWeight="bold" fontFamily="sans-serif">$</text>

      {/* Coin (front) */}
      <ellipse cx="52" cy="82" rx="13" ry="13" fill="#FFCA28" />
      <ellipse cx="52" cy="82" rx="13" ry="13" stroke="#F9A825" strokeWidth="1.5" />
      <ellipse cx="52" cy="82" rx="9" ry="9" fill="#FFCA28" stroke="#F9A825" strokeWidth="1" />
      <text x="52" y="86" textAnchor="middle" fill="#F9A825" fontSize="10" fontWeight="bold" fontFamily="sans-serif">$</text>
    </svg>
  )
}
