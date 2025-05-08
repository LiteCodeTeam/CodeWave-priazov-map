document.querySelector('.registration-form').addEventListener('submit', async function (e) {
    e.preventDefault();

    // Валидация паролей
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('password2').value;

    if (password !== confirmPassword) {
        alert('Пароли не совпадают!');
        return;
    }

    // Сбор данных
    const formData = {
        Name: document.getElementById('organizationName').value.trim(),
        LeaderName: document.getElementById('FIO').value.trim(),
        Phone: document.getElementById('phone').value.replace(/\D/g, ''), // Удаляем все не-цифры
        Email: document.getElementById('mail').value.trim(),
        Password: password,
        //Industry: document.getElementById('industry').value, // Исправленная строка
        Address: document.getElementById('address').value.trim()
    };

    // Валидация обязательных полей
    for (const [key, value] of Object.entries(formData)) {
        if (!value) {
            alert(`Поле ${key} обязательно для заполнения`);
            return;
        }
    }

const requestURL = 'https://jsonplaceholder.typicode.com/users'
// const requestURL = 'http://localhost:5145/api/Company'

    try {
        const response = await fetch(requestURL, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });

        const result = await response.json();

        if (!response.ok) {
            throw new Error(
                result.message ||
                result.errors?.join('\n') ||
                `Ошибка сервера: ${response.status}`
            );
        }

        
        
    } catch (error) {
        console.error('Ошибка регистрации:', error);
        alert(error.message || 'Произошла неизвестная ошибка');
    }
});

// Маска для телефона
document.getElementById('phone').addEventListener('input', function(e) {
let x = e.target.value.replace(/\D/g, '').match(/(\d{0,1})(\d{0,3})(\d{0,3})(\d{0,2})(\d{0,2})/);
e.target.value = !x[2] ? x[1] : x[1] + ' (' + x[2] + ') ' + x[3] + (x[4] ? '-' + x[4] : '') + (x[5] ? '-' + x[5] : '');
});



