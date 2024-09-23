/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ['./Components/**/*.razor'],
    theme: {
        extend: {},
    },
    plugins: [],
    prefix: "tw-",
    corePlugins: {
        preflight: false
    },
}

