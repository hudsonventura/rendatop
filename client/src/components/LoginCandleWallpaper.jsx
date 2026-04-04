import { useEffect, useRef } from "react";

const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

const mulberry32 = (seed) => {
    let state = seed >>> 0;

    return () => {
        state += 0x6D2B79F5;
        let result = Math.imul(state ^ (state >>> 15), state | 1);
        result ^= result + Math.imul(result ^ (result >>> 7), result | 61);
        return ((result ^ (result >>> 14)) >>> 0) / 4294967296;
    };
};

const drawGlow = (ctx, x, y, radius, color, alpha) => {
    const glow = ctx.createRadialGradient(x, y, 0, x, y, radius);
    glow.addColorStop(0, `rgba(${color}, ${alpha})`);
    glow.addColorStop(1, `rgba(${color}, 0)`);
    ctx.fillStyle = glow;
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fill();
};

const LoginCandleWallpaper = () => {
    const wrapperRef = useRef(null);
    const canvasRef = useRef(null);
    const frameRef = useRef(0);
    const resizeFrameRef = useRef(0);
    const seedRef = useRef(Math.floor(Date.now() + Math.random() * 1_000_000));
    const candlesRef = useRef([]);
    const layoutRef = useRef(null);
    const offsetRef = useRef(0);
    const lastTimestampRef = useRef(0);
    const marketRef = useRef({
        price: 92,
        drift: 0,
        segmentLength: 0,
    });

    useEffect(() => {
        const wrapper = wrapperRef.current;
        const canvas = canvasRef.current;

        if (!wrapper || !canvas) {
            return undefined;
        }

        const context = canvas.getContext("2d", { alpha: true });

        if (!context) {
            return undefined;
        }

        const random = mulberry32(seedRef.current);
        marketRef.current.price = 92 + random() * 38;

        const reducedMotionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");

        const getNextCandle = () => {
            const market = marketRef.current;

            if (market.segmentLength <= 0) {
                market.drift = (random() - 0.5) * 5.2;
                market.segmentLength = 3 + Math.floor(random() * 8);
            }

            market.segmentLength -= 1;

            const open = market.price;
            const delta = market.drift + (random() - 0.5) * 8.2;
            const close = Math.max(28, open + delta);
            const wickTop = 1.4 + random() * 6.6;
            const wickBottom = 1.4 + random() * 6.6;
            const high = Math.max(open, close) + wickTop;
            const low = Math.max(12, Math.min(open, close) - wickBottom);

            market.price = close;

            return { open, close, high, low };
        };

        const clearScheduledWork = () => {
            cancelAnimationFrame(frameRef.current);
            cancelAnimationFrame(resizeFrameRef.current);
            lastTimestampRef.current = 0;
        };

        const buildLayout = () => {
            const bounds = wrapper.getBoundingClientRect();
            const width = Math.round(bounds.width);
            const height = Math.round(bounds.height);

            if (width < 24 || height < 24) {
                return null;
            }

            const dpr = clamp(window.devicePixelRatio || 1, 1, 1.5);
            const scaledWidth = Math.round(width * dpr);
            const scaledHeight = Math.round(height * dpr);

            if (canvas.width !== scaledWidth || canvas.height !== scaledHeight) {
                canvas.width = scaledWidth;
                canvas.height = scaledHeight;
            }

            return {
                dpr,
                width,
                height,
                paddingX: Math.max(40, width * 0.09),
                paddingY: Math.max(42, height * 0.16),
                visibleCount: clamp(Math.floor(width / 34), 14, 22),
            };
        };

        const ensureSeries = (targetLength) => {

            if (candlesRef.current.length === 0) {
                candlesRef.current = Array.from({ length: targetLength }, getNextCandle);
                return;
            }

            if (candlesRef.current.length === targetLength) {
                return;
            }

            marketRef.current.price = candlesRef.current[candlesRef.current.length - 1]?.close ?? marketRef.current.price;
            candlesRef.current = Array.from({ length: targetLength }, getNextCandle);
        };

        const draw = (shift = 0) => {
            const layout = buildLayout();

            if (!layout) {
                return;
            }

            layoutRef.current = layout;

            const { dpr, width, height, paddingX, paddingY, visibleCount } = layout;
            const plotWidth = width - paddingX * 2;
            const plotHeight = height - paddingY * 2;
            const stepX = visibleCount > 1 ? plotWidth / (visibleCount - 1) : plotWidth;
            const bodyWidth = clamp(stepX * 0.62, 12, 22);
            const leftBufferCount = Math.ceil((paddingX + bodyWidth) / stepX) + 1;
            const rightBufferCount = 3;
            const startX = paddingX - stepX * leftBufferCount;
            const targetLength = visibleCount + leftBufferCount + rightBufferCount;

            ensureSeries(targetLength);

            const candles = candlesRef.current;
            const highs = candles.map((candle) => candle.high);
            const lows = candles.map((candle) => candle.low);
            const maxPrice = Math.max(...highs);
            const minPrice = Math.min(...lows);
            const priceRange = Math.max(1, maxPrice - minPrice);
            const toY = (price) => paddingY + ((maxPrice - price) / priceRange) * plotHeight;

            context.setTransform(1, 0, 0, 1, 0, 0);
            context.clearRect(0, 0, canvas.width, canvas.height);
            context.setTransform(dpr, 0, 0, dpr, 0, 0);

            const background = context.createLinearGradient(0, 0, 0, height);
            background.addColorStop(0, "#020617");
            background.addColorStop(0.5, "#06111e");
            background.addColorStop(1, "#01030a");
            context.fillStyle = background;
            context.fillRect(0, 0, width, height);

            drawGlow(context, width * 0.18, height * 0.2, width * 0.38, "34, 197, 94", 0.16);
            drawGlow(context, width * 0.84, height * 0.72, width * 0.44, "14, 165, 233", 0.14);
            drawGlow(context, width * 0.52, height * 0.34, width * 0.24, "255, 255, 255", 0.06);

            context.strokeStyle = "rgba(148, 163, 184, 0.08)";
            context.lineWidth = 1;

            for (let index = 1; index < 6; index += 1) {
                const x = (width / 6) * index;
                context.beginPath();
                context.moveTo(x, 0);
                context.lineTo(x, height);
                context.stroke();
            }

            for (let index = 1; index < 5; index += 1) {
                const y = (height / 5) * index;
                context.beginPath();
                context.moveTo(0, y);
                context.lineTo(width, y);
                context.stroke();
            }

            context.save();
            context.beginPath();
            context.rect(0, 0, width, height);
            context.clip();

            context.beginPath();
            candles.forEach((candle, index) => {
                const x = startX + stepX * index - shift;
                const y = toY(candle.close);

                if (index === 0) {
                    context.moveTo(x, y);
                    return;
                }

                const previousX = startX + stepX * (index - 1) - shift;
                const previousY = toY(candles[index - 1].close);
                const controlX = (previousX + x) / 2;

                context.quadraticCurveTo(previousX, previousY, controlX, (previousY + y) / 2);
                context.quadraticCurveTo(controlX, (previousY + y) / 2, x, y);
            });
            context.strokeStyle = "rgba(226, 232, 240, 0.16)";
            context.lineWidth = 1.5;
            context.shadowColor = "rgba(226, 232, 240, 0.12)";
            context.shadowBlur = 28;
            context.stroke();
            context.shadowBlur = 0;

            candles.forEach((candle, index) => {
                const x = startX + stepX * index - shift;

                if (x < -bodyWidth * 2 || x > width + bodyWidth * 2) {
                    return;
                }

                const openY = toY(candle.open);
                const closeY = toY(candle.close);
                const highY = toY(candle.high);
                const lowY = toY(candle.low);
                const bullish = candle.close >= candle.open;
                const bodyTop = Math.min(openY, closeY);
                const bodyHeight = Math.max(Math.abs(closeY - openY), 3);
                const color = bullish ? "rgba(52, 211, 153, 0.96)" : "rgba(248, 113, 113, 0.94)";
                const glow = bullish ? "rgba(16, 185, 129, 0.46)" : "rgba(239, 68, 68, 0.42)";

                context.strokeStyle = color;
                context.lineWidth = 1.4;
                context.beginPath();
                context.moveTo(x, highY);
                context.lineTo(x, lowY);
                context.stroke();

                context.shadowColor = glow;
                context.shadowBlur = 26;
                context.fillStyle = color;
                context.fillRect(x - bodyWidth / 2, bodyTop, bodyWidth, bodyHeight);
                context.shadowBlur = 0;
            });

            context.restore();

            const vignette = context.createRadialGradient(
                width * 0.5,
                height * 0.42,
                height * 0.12,
                width * 0.5,
                height * 0.42,
                Math.max(width, height) * 0.8
            );
            vignette.addColorStop(0, "rgba(2, 6, 23, 0)");
            vignette.addColorStop(1, "rgba(2, 6, 23, 0.64)");
            context.fillStyle = vignette;
            context.fillRect(0, 0, width, height);
        };

        const startAnimationLoop = () => {
            if (reducedMotionQuery.matches || document.hidden) {
                draw(offsetRef.current);
                return;
            }

            const animate = (timestamp) => {
                const currentLayout = layoutRef.current || buildLayout();

                if (!currentLayout) {
                    lastTimestampRef.current = timestamp;
                    frameRef.current = window.requestAnimationFrame(animate);
                    return;
                }

                const plotWidth = currentLayout.width - currentLayout.paddingX * 2;
                const stepX = currentLayout.visibleCount > 1
                    ? plotWidth / (currentLayout.visibleCount - 1)
                    : plotWidth;

                if (!lastTimestampRef.current) {
                    lastTimestampRef.current = timestamp;
                }

                const deltaSeconds = Math.min((timestamp - lastTimestampRef.current) / 1000, 0.05);
                lastTimestampRef.current = timestamp;
                const speed = stepX * 0.55;

                offsetRef.current += speed * deltaSeconds;

                while (offsetRef.current >= stepX) {
                    offsetRef.current -= stepX;
                    candlesRef.current.shift();
                    candlesRef.current.push(getNextCandle());
                }

                draw(offsetRef.current);
                frameRef.current = window.requestAnimationFrame(animate);
            };

            frameRef.current = window.requestAnimationFrame(animate);
        };

        const redraw = () => {
            clearScheduledWork();
            draw(offsetRef.current);
            startAnimationLoop();
        };

        const scheduleRedraw = () => {
            cancelAnimationFrame(resizeFrameRef.current);
            resizeFrameRef.current = window.requestAnimationFrame(redraw);
        };

        const handleVisibilityChange = () => {
            redraw();
        };

        const handleReducedMotionChange = () => {
            redraw();
        };

        redraw();

        let observer;

        if (typeof ResizeObserver !== "undefined") {
            observer = new ResizeObserver(scheduleRedraw);
            observer.observe(wrapper);
        } else {
            window.addEventListener("resize", scheduleRedraw);
        }

        document.addEventListener("visibilitychange", handleVisibilityChange);
        reducedMotionQuery.addEventListener("change", handleReducedMotionChange);

        return () => {
            clearScheduledWork();
            observer?.disconnect();
            window.removeEventListener("resize", scheduleRedraw);
            document.removeEventListener("visibilitychange", handleVisibilityChange);
            reducedMotionQuery.removeEventListener("change", handleReducedMotionChange);
        };
    }, []);

    return (
        <div ref={wrapperRef} className="absolute inset-0 overflow-hidden bg-slate-950" aria-hidden="true">
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,_rgba(52,211,153,0.16),_transparent_34%),radial-gradient(circle_at_bottom_right,_rgba(14,165,233,0.18),_transparent_38%)]" />
            <canvas
                ref={canvasRef}
                className="absolute inset-0 h-full w-full opacity-90"
                style={{ filter: "blur(6.8px) saturate(1.15)", transform: "scale(1.08)" }}
            />
            <div className="absolute inset-0 bg-gradient-to-br from-slate-950/10 via-slate-950/32 to-slate-950/82" />
            <div className="absolute -left-16 top-12 h-56 w-56 rounded-full bg-emerald-400/20 blur-3xl" />
            <div className="absolute bottom-0 right-0 h-72 w-72 translate-x-16 translate-y-12 rounded-full bg-sky-400/20 blur-3xl" />
        </div>
    );
};

export default LoginCandleWallpaper;
