export function formatPrice(centimes: number): string {
    return (centimes / 100).toLocaleString("fr-DZ", { 
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    })+" DA";
}