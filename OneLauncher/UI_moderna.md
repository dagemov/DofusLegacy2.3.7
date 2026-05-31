<!DOCTYPE html>

<html class="dark" lang="fr"><head>
<meta charset="utf-8"/>
<meta content="width=device-width, initial-scale=1.0" name="viewport"/>
<title>Dofus - Mystic Forge Launcher</title>
<script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
<link href="https://fonts.googleapis.com/css2?family=Epilogue:wght@400;600;700;800&amp;family=Work+Sans:wght@400;500&amp;family=JetBrains+Mono:wght@400;500&amp;family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&amp;display=swap" rel="stylesheet"/>
<link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&amp;display=swap" rel="stylesheet"/>
<link href="https://fonts.googleapis.com/css2?family=Epilogue:wght@100..900&amp;family=JetBrains+Mono:wght@100..900&amp;family=Work+Sans:wght@100..900&amp;display=swap" rel="stylesheet"/>
<style>
        .material-symbols-outlined {
            font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
        }
        
        .skeuo-panel {
            background: linear-gradient(180deg, #2a2a27 0%, #1c1c19 100%);
            box-shadow: inset 0 1px 1px rgba(255, 255, 255, 0.05), 0 10px 20px rgba(0,0,0,0.5);
            border: 1px solid #564336;
        }

        .skeuo-button-primary {
            background: linear-gradient(180deg, #f48221 0%, #964900 100%);
            border-top: 1px solid #ffdcc6;
            box-shadow: 0 0 15px rgba(244, 130, 33, 0.4);
        }

        .skeuo-button-primary:hover {
            box-shadow: 0 0 25px rgba(244, 130, 33, 0.6);
            transform: translateY(-1px);
        }

        .progress-channel {
            background: #0e0e0c;
            box-shadow: inset 0 2px 4px rgba(0,0,0,0.8);
        }

        .progress-fill {
            background: linear-gradient(90deg, #964900 0%, #f48221 50%, #ffb786 100%);
            box-shadow: 0 0 10px rgba(244, 130, 33, 0.5);
            position: relative;
            overflow: hidden;
        }

        .progress-fill::after {
            content: '';
            position: absolute;
            top: 0; left: 0; right: 0; bottom: 0;
            background: linear-gradient(180deg, rgba(255,255,255,0.2) 0%, transparent 50%, rgba(0,0,0,0.2) 100%);
        }

        .custom-scrollbar::-webkit-scrollbar {
            width: 4px;
        }
        .custom-scrollbar::-webkit-scrollbar-track {
            background: #0e0e0c;
        }
        .custom-scrollbar::-webkit-scrollbar-thumb {
            background: #a58c7d;
            border-radius: 2px;
        }

        .ornate-corner {
            position: absolute;
            width: 12px;
            height: 12px;
            border: 2px solid #b2d43a;
            box-shadow: 0 0 8px #b2d43a;
        }
    </style>
<script id="tailwind-config">
      tailwind.config = {
        darkMode: "class",
        theme: {
          extend: {
            "colors": {
                    "surface-dim": "#131411",
                    "primary-fixed-dim": "#ffb786",
                    "surface": "#131411",
                    "surface-container": "#20201d",
                    "on-secondary-fixed": "#171e00",
                    "background": "#131411",
                    "on-error": "#690005",
                    "on-primary-fixed": "#311300",
                    "inverse-surface": "#e5e2dd",
                    "on-primary": "#502400",
                    "on-surface-variant": "#ddc1b0",
                    "tertiary-fixed-dim": "#d6c68c",
                    "on-tertiary": "#393004",
                    "error-container": "#93000a",
                    "primary-fixed": "#ffdcc6",
                    "on-primary-container": "#5a2a00",
                    "error": "#ffb4ab",
                    "secondary-fixed-dim": "#b2d43a",
                    "secondary-fixed": "#cdf155",
                    "secondary-container": "#88a700",
                    "inverse-on-surface": "#31302d",
                    "surface-container-low": "#1c1c19",
                    "on-tertiary-container": "#403709",
                    "inverse-primary": "#964900",
                    "tertiary": "#d6c68c",
                    "surface-container-lowest": "#0e0e0c",
                    "secondary": "#b2d43a",
                    "surface-bright": "#393936",
                    "primary": "#ffb786",
                    "outline": "#a58c7d",
                    "on-secondary-fixed-variant": "#3d4d00",
                    "surface-tint": "#ffb786",
                    "tertiary-container": "#aea069",
                    "on-secondary-container": "#2b3700",
                    "on-tertiary-fixed-variant": "#514618",
                    "surface-variant": "#353532",
                    "tertiary-fixed": "#f3e2a5",
                    "on-primary-fixed-variant": "#723600",
                    "on-surface": "#e5e2dd",
                    "on-secondary": "#293500",
                    "surface-container-highest": "#353532",
                    "on-tertiary-fixed": "#221b00",
                    "outline-variant": "#564336",
                    "surface-container-high": "#2a2a27",
                    "primary-container": "#f48221",
                    "on-error-container": "#ffdad6",
                    "on-background": "#e5e2dd"
            },
            "borderRadius": {
                    "DEFAULT": "0.25rem",
                    "lg": "0.5rem",
                    "xl": "0.75rem",
                    "full": "9999px"
            },
            "spacing": {
                    "inner-padding": "12px",
                    "gutter": "16px",
                    "container-padding": "24px",
                    "base": "8px"
            },
            "fontFamily": {
                    "label-md": ["JetBrains Mono"],
                    "headline-lg": ["Epilogue"],
                    "headline-md": ["Epilogue"],
                    "body-lg": ["Work Sans"],
                    "body-sm": ["Work Sans"],
                    "status-code": ["JetBrains Mono"]
            },
            "fontSize": {
                    "label-md": ["12px", {"lineHeight": "16px", "fontWeight": "500"}],
                    "headline-lg": ["32px", {"lineHeight": "40px", "letterSpacing": "-0.02em", "fontWeight": "700"}],
                    "headline-md": ["20px", {"lineHeight": "28px", "fontWeight": "600"}],
                    "body-lg": ["16px", {"lineHeight": "24px", "fontWeight": "400"}],
                    "body-sm": ["14px", {"lineHeight": "20px", "fontWeight": "400"}],
                    "status-code": ["11px", {"lineHeight": "14px", "fontWeight": "400"}]
            }
          }
        }
      }
    </script>
<style>
    body {
      min-height: max(884px, 100dvh);
    }
  </style>
  </head>
<body class="bg-surface-dim text-on-surface min-h-screen flex flex-col font-body-lg overflow-hidden">
<!-- TopAppBar -->
<header class="bg-surface-container-high border-b border-outline-variant shadow-md flex justify-between items-center px-container-padding py-base w-full z-50">
<div class="flex items-center gap-base">
<img alt="Dofus Logo" class="w-8 h-8 filter drop-shadow-[0_0_5px_#b2d43a]" src="https://lh3.googleusercontent.com/aida-public/AB6AXuA-8Zl3wIy3XhRf30BAtcW0yc3AZqlBAVZvGuWjjEuEyac3iWGyhxlB1Fxho1P5c9UM9XC8JJfDU3jsqtHe2B5Ho4p4QRSCjxGJeEmvD02wSSrz2aX-dAXBD8HUE0St0QGFgnijLV7eImjHKiUpPwzwr9YfjZscOW0WAJ1oLa5w1kQyg78t8bOgSKb6qLXnFYX-ZYYbeHzsJKgK1daNpdDfyiarTdUmRq9AeSGo1nQKS62nCQR6SZfk1TsZed6lwqQdy8QUNR3njYc"/>
<h1 class="font-headline-md text-headline-md font-bold text-on-surface tracking-tight">Dofus</h1>
</div>
<div class="flex items-center gap-gutter">
<button class="text-on-surface-variant hover:text-primary transition-colors duration-200 flex items-center gap-1">
<span class="w-5 h-3 bg-blue-700 relative block border border-white/20">
<span class="absolute left-1/3 top-0 bottom-0 w-1/3 bg-white"></span>
<span class="absolute left-2/3 top-0 bottom-0 w-1/3 bg-red-600"></span>
</span>
<span class="font-label-md text-label-md">FR</span>
</button>
<div class="flex gap-base">
<button class="material-symbols-outlined text-on-surface-variant hover:text-on-surface transition-colors">minimize</button>
<button class="material-symbols-outlined text-on-surface-variant hover:text-error transition-colors">close</button>
</div>
</div>
</header>
<div class="flex flex-1 relative">
<!-- Navigation Drawer (Side) -->
<aside class="hidden md:flex flex-col gap-base py-container-padding bg-surface-container-low border-r border-outline-variant w-64 shadow-2xl z-40">
<div class="px-container-padding mb-base">
<h2 class="font-headline-lg text-headline-lg text-primary tracking-tighter">MYSTIC FORGE</h2>
</div>
<nav class="flex flex-col gap-1">
<a class="flex items-center gap-inner-padding py-3 px-container-padding bg-primary-container text-on-primary-container rounded-lg mx-2 transition-all shadow-[0_0_15px_rgba(244,130,33,0.3)]" href="#">
<span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1;">home</span>
<span class="font-label-md text-label-md">Home</span>
</a>
<a class="flex items-center gap-inner-padding py-3 px-container-padding text-on-surface-variant hover:bg-surface-variant rounded-lg mx-2 transition-all" href="#">
<span class="material-symbols-outlined">article</span>
<span class="font-label-md text-label-md">News</span>
</a>
<a class="flex items-center gap-inner-padding py-3 px-container-padding text-on-surface-variant hover:bg-surface-variant rounded-lg mx-2 transition-all" href="#">
<span class="material-symbols-outlined">history</span>
<span class="font-label-md text-label-md">Logs</span>
</a>
<a class="flex items-center gap-inner-padding py-3 px-container-padding text-on-surface-variant hover:bg-surface-variant rounded-lg mx-2 transition-all" href="#">
<span class="material-symbols-outlined">settings</span>
<span class="font-label-md text-label-md">Settings</span>
</a>
</nav>
</aside>
<!-- Main Content Canvas -->
<main class="flex-1 relative overflow-hidden flex flex-col">
<!-- Hero Illustration Layer -->
<div class="absolute inset-0 z-0">
<img class="w-full h-full object-cover opacity-60 mix-blend-luminosity" data-alt="A lush, vibrant fantasy landscape featuring towering ancient trees and mystical floating islands. The scene is bathed in a magical golden-green glow that filters through thick emerald foliage. In the foreground, stylized high-fantasy characters with expressive features stand ready for adventure. The overall aesthetic is painterly and epic, mirroring the colorful and handcrafted world of a high-end MMORPG launcher." src="https://lh3.googleusercontent.com/aida-public/AB6AXuAKM9Ftiv_Xdx6u0unce8QzebAigP3HGS4kTe5e1ykI3qxhr_qyVfJ1koHn4GikPEPznldn4Vw8ugz6Mztu5zN5Bych5qFArqVi5J48X9U9G5LB3MfT9uUOaTqu2Rgv8YKNorOu-pKG0xhVoZXkhayJfZxh6PamhvcJtK_HQad3v2oL_g2L54pEFwdH4CsV8az902XGV9jPiNY3oQG5qTU_vp9YtH7mrZh8xZWTX9CfTFNIX0542k0q963HqznHX5QgV03PRkAbX-M"/>
<div class="absolute inset-0 bg-gradient-to-t from-surface-dim via-transparent to-transparent"></div>
<div class="absolute inset-0 bg-gradient-to-r from-surface-dim/80 via-transparent to-transparent"></div>
</div>
<!-- Bento Layout Content -->
<div class="relative z-10 flex-1 p-container-padding flex flex-col gap-gutter">
<div class="flex-1 grid grid-cols-12 grid-rows-6 gap-gutter">
<!-- Featured Event Card -->
<div class="col-span-8 row-span-4 skeuo-panel rounded-xl p-container-padding flex flex-col justify-end relative overflow-hidden group">
<div class="absolute inset-0 opacity-40 group-hover:scale-110 transition-transform duration-700 pointer-events-none">
<img class="w-full h-full object-cover" data-alt="A dynamic action scene from a fantasy game featuring a massive, glowing green dragon rising from an emerald forest. The lighting is dramatic, with neon green energy crackling through the air. The style is sharp and digital, with high contrast between deep shadows and brilliant lime-green magical highlights. The atmosphere is one of intense magical power and high-stakes adventure." src="https://lh3.googleusercontent.com/aida-public/AB6AXuDl3qzf8UKMOQTvii7PkX-WfzNRigPJi_jpRuBH96HnoiYnpcWfnvdgRYgG5-U1f-kD_NdXIQyP9NgwU9f8ZHn0_Y74R5FrgNOW-A1eWFrIYLbTtESl740wpQYuDgTaloXDvWRhTMx11UibcHTUKEof3o3H0RR9T-48r4bHZW6xsS3Tb0zIiPlgIh1yYftR0lvnqeENDEmrzLnub0NuDswzpDjlaKe7GlAmBFCuAWjC3x3kAawIPFSb-X5RDK_zTWR4SMkus_7ZLsI"/>
</div>
<div class="absolute inset-0 bg-gradient-to-t from-black/80 to-transparent"></div>
<div class="relative">
<span class="bg-secondary text-on-secondary px-base py-1 rounded text-label-md font-label-md mb-base inline-block">EVENTO ESPECIAL</span>
<h2 class="font-headline-lg text-headline-lg text-on-surface mb-2">¡La venganza del Dofus Esmeralda!</h2>
<p class="text-on-surface-variant max-w-lg mb-gutter font-body-sm">Descubre las nuevas misiones de temporada y obtén recompensas exclusivas por tiempo limitado en esta actualización masiva.</p>
<button class="skeuo-button-primary px-container-padding py-base rounded-lg text-on-primary font-label-md hover:scale-105 transition-transform">
                                SABER MÁS
                            </button>
</div>
</div>
<!-- Side Profile Info -->
<div class="col-span-4 row-span-2 skeuo-panel rounded-xl p-inner-padding flex flex-col items-center justify-center text-center">
<div class="w-16 h-16 rounded-full border-2 border-secondary-fixed-dim p-1 mb-base shadow-[0_0_10px_#b2d43a]">
<img alt="Avatar" class="w-full h-full rounded-full bg-surface-container" src="https://lh3.googleusercontent.com/aida-public/AB6AXuAKnWmeS2_PjzsmSAsUeS4S_VPbadTB52k2_6LFZIqmgTvj4dyglcU9f4KpKwA7YqJflBPviarzG7bpHm_0zGhNkS2C-vqnKYczUvSI5Cm0dPK14MihkcvIRynxbYwiJjSVxjMlY0t2N1OqnGXnjTsxfWtylq1qmipRIdCVyCQdNHW9J3H2tJQ2RzCWRKOciQJ_CqgyXs-cP_Pds18Ubj2Gd16UeGHkIVTl1Qx9L070esHcbIJOMsGdfG6NEqj9q3bzIFW64dsjmMI"/>
</div>
<h3 class="text-on-surface font-headline-md text-headline-md">Xelor_Master</h3>
<div class="flex items-center gap-2 mt-2">
<span class="w-2 h-2 rounded-full bg-secondary animate-pulse"></span>
<span class="text-secondary text-label-md font-label-md uppercase tracking-wider">Level 200</span>
</div>
</div>
<!-- Logs Panel -->
<div class="col-span-4 row-span-4 skeuo-panel rounded-xl flex flex-col overflow-hidden">
<div class="flex bg-surface-container-high">
<button class="flex-1 py-3 text-on-surface-variant font-label-md hover:bg-surface-variant transition-colors">Noticias</button>
<button class="flex-1 py-3 bg-surface-container-low text-primary-fixed-dim font-label-md border-b-2 border-primary-container">Registros</button>
</div>
<div class="flex-1 p-inner-padding font-status-code text-status-code custom-scrollbar overflow-y-auto bg-surface-container-lowest/50 backdrop-blur-md">
<p class="text-secondary-fixed-dim mb-1">[08:45:22] Iniciando protocolo de actualización...</p>
<p class="text-on-surface-variant/80 mb-1">Téléchargement de patch_0.0_2.0.0.22554_base.zip</p>
<p class="text-on-surface-variant/80 mb-1">Vitesse de téléchargement : 4.52 Mo/s (842.12 Mo/1024.03 Mo)</p>
<p class="text-on-surface-variant/80 mb-1">Téléchargement de patch_2.0.0.22554_2.0.0.22877_base.zip</p>
<p class="text-on-surface-variant/80 mb-1">Vitesse de téléchargement : 5.12 Mo/s (128.50 Mo/128.99 Mo)</p>
<p class="text-on-surface-variant/80 mb-1">Vérification de l'intégrité des fichiers locaux...</p>
<p class="text-on-surface-variant/80 mb-1">Décompression de l'archive system_core.pkg</p>
<p class="text-on-surface-variant/80 mb-1">Installation de patch_0.0_2.0.0.22554_base.zip...</p>
<p class="text-secondary-fixed-dim mb-1">[08:47:10] Actualización del 83% completada.</p>
<p class="text-on-surface-variant/80">Esperando respuesta del servidor de parches...</p>
</div>
</div>
<!-- Mini Stat Cards -->
<div class="col-span-4 row-span-2 skeuo-panel rounded-xl p-inner-padding flex items-center justify-between group">
<div>
<p class="text-on-surface-variant font-label-md text-label-md">Servidor Eratz</p>
<p class="text-secondary font-headline-md text-headline-md">ESTABLE</p>
</div>
<span class="material-symbols-outlined text-secondary text-4xl group-hover:scale-110 transition-transform" style="font-variation-settings: 'FILL' 1;">dns</span>
</div>
<div class="col-span-4 row-span-2 skeuo-panel rounded-xl p-inner-padding flex items-center justify-between group">
<div>
<p class="text-on-surface-variant font-label-md text-label-md">Abono Restante</p>
<p class="text-primary font-headline-md text-headline-md">15 Días</p>
</div>
<span class="material-symbols-outlined text-primary text-4xl group-hover:scale-110 transition-transform" style="font-variation-settings: 'FILL' 1;">auto_awesome</span>
</div>
</div>
<!-- Footer Update Console -->
<div class="mt-auto flex items-end gap-gutter pt-gutter">
<div class="flex-1 flex flex-col gap-2">
<div class="flex justify-between items-end">
<span class="font-label-md text-label-md text-primary-fixed-dim animate-pulse">Actualización del juego en curso...</span>
<span class="font-label-md text-label-md text-on-surface-variant">83% (1.2 GB / 1.5 GB)</span>
</div>
<div class="progress-channel h-8 rounded-full border border-outline-variant relative overflow-hidden">
<div class="progress-fill h-full rounded-full w-[83%] transition-all duration-500 ease-out flex items-center justify-center">
<div class="w-full h-full absolute inset-0 bg-[linear-gradient(45deg,rgba(255,255,255,0.1)_25%,transparent_25%,transparent_50%,rgba(255,255,255,0.1)_50%,rgba(255,255,255,0.1)_75%,transparent_75%,transparent)] bg-[length:20px_20px] animate-[scroll_2s_linear_infinite]"></div>
</div>
</div>
</div>
<div class="relative group">
<div class="absolute -inset-1 bg-primary-container rounded-lg blur opacity-25 group-hover:opacity-50 transition duration-1000 group-hover:duration-200"></div>
<button class="skeuo-button-primary relative px-12 py-5 rounded-lg flex flex-col items-center justify-center gap-1 min-w-[220px]">
<span class="font-headline-md text-headline-md text-on-primary font-extrabold uppercase tracking-widest">ACTUALIZANDO</span>
<span class="font-label-md text-label-md text-on-primary-container/80 uppercase">Por favor espera</span>
</button>
</div>
<button class="skeuo-panel p-inner-padding rounded-lg text-on-surface-variant hover:text-primary transition-all hover:rotate-90 duration-500">
<span class="material-symbols-outlined text-3xl">settings</span>
</button>
</div>
</div>
</main>
</div>
<!-- Mobile Navigation -->
<footer class="md:hidden fixed bottom-0 left-0 w-full flex justify-center gap-gutter bg-surface-container shadow-inner border-t border-secondary-container p-inner-padding z-50">
<button class="flex flex-col items-center justify-center text-on-surface-variant p-inner-padding">
<span class="material-symbols-outlined">rss_feed</span>
<span class="font-label-md text-label-md">News</span>
</button>
<button class="flex flex-col items-center justify-center bg-secondary-container text-on-secondary-container rounded-t-xl p-inner-padding shadow-[0_-5px_15px_rgba(136,167,0,0.3)]">
<span class="material-symbols-outlined" style="font-variation-settings: 'FILL' 1;">terminal</span>
<span class="font-label-md text-label-md">Logs</span>
</button>
</footer>
<style>
        @keyframes scroll {
            from { background-position: 0 0; }
            to { background-position: 40px 0; }
        }
    </style>
<script>
        // Micro-interactions and atmospheric effects
        document.addEventListener('mousemove', (e) => {
            const x = e.clientX / window.innerWidth;
            const y = e.clientY / window.innerHeight;
            
            // Subtle parallax for the background image
            const bg = document.querySelector('main > div:first-child');
            if (bg) {
                bg.style.transform = `translate(${(x - 0.5) * 10}px, ${(y - 0.5) * 10}px)`;
            }
        });

        // Toggle logs scroll effect simulation
        const logPanel = document.querySelector('.custom-scrollbar');
        if (logPanel) {
            setInterval(() => {
                logPanel.scrollTop += 1;
                if (logPanel.scrollTop >= logPanel.scrollHeight - logPanel.clientHeight) {
                    logPanel.scrollTop = 0;
                }
            }, 50);
        }
    </script>
</body></html>