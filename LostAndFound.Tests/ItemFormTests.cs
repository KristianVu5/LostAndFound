using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using LostAndFound.ViewModels;
using Xunit;

namespace LostAndFound.Tests
{
    public class ItemFormTests
    {

        [Theory]
        [InlineData(120, true)]
        [InlineData(121, false)]
        public void Title_ValidatesMaximumLength(int length, bool expectedValid)
        {
            var form = new ItemForm { Title = new string('A', length) };
            var context = new ValidationContext(form)
            {
                MemberName = nameof(ItemForm.Title)
            };

            var isValid = Validator.TryValidateProperty(form.Title, context, null);

            Assert.Equal(expectedValid, isValid);
        }
    }
}
