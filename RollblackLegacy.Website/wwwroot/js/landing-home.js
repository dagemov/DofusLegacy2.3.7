(function () {
    const navbar = document.getElementById("navbar");
    if (navbar) {
        window.addEventListener("scroll", () => {
            navbar.classList.toggle("scrolled", window.scrollY > 50);
        });
    }

    const particlesContainer = document.getElementById("particles");
    if (particlesContainer) {
        for (let i = 0; i < 30; i++) {
            const p = document.createElement("div");
            p.className = "particle";
            p.style.left = Math.random() * 100 + "%";
            p.style.animationDelay = Math.random() * 15 + "s";
            p.style.animationDuration = 10 + Math.random() * 10 + "s";
            particlesContainer.appendChild(p);
        }
    }

    function showReveals() {
        document.querySelectorAll(".reveal").forEach((el) => {
            el.style.opacity = "1";
            el.style.transform = "none";
        });
    }

    if (typeof gsap === "undefined" || typeof ScrollTrigger === "undefined") {
        showReveals();
        return;
    }

    gsap.registerPlugin(ScrollTrigger);

    gsap.from(".hero-badge", { opacity: 0, y: 20, duration: 0.8, delay: 0.2, ease: "power3.out" });
    gsap.from(".hero-logo", { opacity: 0, scale: 0.8, duration: 1, delay: 0.4, ease: "elastic.out(1, 0.5)" });
    gsap.from(".hero-title", { opacity: 0, y: 30, duration: 0.8, delay: 0.6, ease: "power3.out" });
    gsap.from(".hero-subtitle", { opacity: 0, y: 20, duration: 0.8, delay: 0.8, ease: "power3.out" });
    gsap.from(".hero-cta", { opacity: 0, y: 20, duration: 0.8, delay: 1, ease: "power3.out" });

    document.querySelectorAll(".reveal").forEach((el) => {
        gsap.to(el, {
            opacity: 1,
            y: 0,
            duration: 0.8,
            ease: "power3.out",
            scrollTrigger: {
                trigger: el,
                start: "top 85%",
                toggleActions: "play none none none",
            },
        });
    });

    document.querySelectorAll(".step-visual img").forEach((img) => {
        gsap.to(img, {
            y: -20,
            ease: "none",
            scrollTrigger: {
                trigger: img,
                start: "top bottom",
                end: "bottom top",
                scrub: 1,
            },
        });
    });

    ScrollTrigger.addEventListener("refreshInit", showReveals);
    window.setTimeout(showReveals, 2500);
})();
