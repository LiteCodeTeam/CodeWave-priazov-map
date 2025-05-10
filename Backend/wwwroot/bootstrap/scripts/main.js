document.addEventListener('DOMContentLoaded', function() {
    const faqItems = document.querySelectorAll('.faq-item');
    
    faqItems.forEach(item => {
        const question = item.querySelector('.faq-question');
        const arrow = item.querySelector('.farrow');
        
        question.addEventListener('click', () => {
            // Close all other items
            faqItems.forEach(otherItem => {
                if (otherItem !== item && otherItem.classList.contains('active')) {
                    otherItem.classList.remove('active');
                    otherItem.querySelector('.farrow').classList.remove('farrow-rotated');
                }
            });
            
            // Toggle current item
            item.classList.toggle('active');
            arrow.classList.toggle('farrow-rotated');
        });
    });
});